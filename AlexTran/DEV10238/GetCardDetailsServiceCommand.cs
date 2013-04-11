using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Contracts.Shared.Records;
using Common.Types;
using Payjr.Core.Adapters;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using Payjr.Entity;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Core.BrandingSite;
using Common.Business.Validation;

namespace Payjr.Core.ServiceCommands.Prepaid
{
    public class GetCardDetailsServiceCommand : ProviderServiceCommandBase<PrepaidCardSearchRequest, PrepaidCardSearchResponse>
    {
        private List<PrepaidCardSearchCriteria> cardSearchCriteria;
        private ICardProvider _cardProvider;

        public GetCardDetailsServiceCommand(IProviderFactory providerFactory)
            : base(providerFactory)
        {
        }

        public ICardProvider CardProvider
        {
            get
            {
                return _cardProvider;
            }
            set
            {
                _cardProvider = value;
            }
        }

        protected override void Validate(PrepaidCardSearchRequest request)
        {
            if (request == null)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request is null");
                throw new ArgumentNullException( "request","request must be set");
            }
            if (request.Requests.Count == 0)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request.Requests has not any item");
                throw new ArgumentException("AddCardsRecords must be set", "request");
            }

            cardSearchCriteria = request.Requests;
        }

        protected override bool OnExecute(PrepaidCardSearchResponse response)
        {
            try
            {
                foreach (PrepaidCardSearchCriteria prepaidCardSearch in cardSearchCriteria)
                {
                    if (!string.IsNullOrWhiteSpace(prepaidCardSearch.PrepaidCardIdentifier))
                    {
                        Guid prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(prepaidCardSearch.PrepaidCardIdentifier).PersistableID;                        
                        UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByPrepaidCardIdentifier(prepaidCardID);
                            if (user != null && user.RoleType == RoleType.RegisteredTeen)
                            {
                                Teen teen = new Teen((RegisteredTeenEntity)user);
                                if (CardProvider != null)
                                {
                                    teen.CardProvider = CardProvider;
                                }
                                FinancialAccountList<PrepaidCardAccount> account = teen.FinancialAccounts.PrepaidCardAccounts;
                                foreach (PrepaidCardAccount prepaidCardAccount in account)
                                {
                                    if (prepaidCardAccount != null)
                                    {
                                        PrepaidCardDetailRecord cardDetail = GetPrepaidCardDetailRecordbyPrepaidCardAccount(prepaidCardAccount, teen);
                                        response.Records.Add(cardDetail);
                                    }
                                }                                
                            }                        
                    }
                    else if (!string.IsNullOrWhiteSpace(prepaidCardSearch.CardNumberFull))
                    {
                        UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByPrepaidCardNumber(prepaidCardSearch.CardNumberFull);
                        if (user != null && user.RoleType == RoleType.RegisteredTeen)
                        {
                            Teen teen = new Teen((RegisteredTeenEntity)user);
                            if (CardProvider != null)
                            {
                                teen.CardProvider = CardProvider;
                            }
                            FinancialAccountList<PrepaidCardAccount> account = teen.FinancialAccounts.PrepaidCardAccounts;
                            PrepaidCardAccount prepaidCardAccount = account.GetPrepaidCardAccountByCardNumber(prepaidCardSearch.CardNumberFull);
                            if (prepaidCardAccount != null)
                            {
                                PrepaidCardDetailRecord cardDetail = GetPrepaidCardDetailRecordbyPrepaidCardAccount(prepaidCardAccount, teen);
                                response.Records.Add(cardDetail);                  
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(prepaidCardSearch.UserIdentifier))
                    {
                        Guid userId = new Identifiers.UserIdentifier(prepaidCardSearch.UserIdentifier).ID;
                        UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByUserID(userId.ToString());
                        if (user != null && user.RoleType == RoleType.RegisteredTeen)
                        {
                            Teen teen = new Teen((RegisteredTeenEntity)user);
                            if (CardProvider != null)
                            {
                                teen.CardProvider = CardProvider;
                            }
                            FinancialAccountList<PrepaidCardAccount> account = teen.FinancialAccounts.PrepaidCardAccounts;
                            foreach (PrepaidCardAccount prepaidCardAccount in account)
                            {
                                if (prepaidCardAccount != null)
                                {
                                    PrepaidCardDetailRecord cardDetail = GetPrepaidCardDetailRecordbyPrepaidCardAccount(prepaidCardAccount, teen);
                                    response.Records.Add(cardDetail);
                                }
                            }                 
                        }    
                      
                    }              
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error when trying to get card detail", ex);
                return false;
            }

        }


        #region Helper
        private PrepaidCardDetailRecord GetPrepaidCardDetailRecordbyPrepaidCardAccount(PrepaidCardAccount prepaidCardAccount, Teen teen)
        {
            PrepaidCardStatus2 cardStatus2 = ConvertToPrepaidCardStatus2(prepaidCardAccount.Status);
            decimal? balance = prepaidCardAccount.GetBalance();
            EntityCollection<JournalEntity> journalEntities = prepaidCardAccount.GetJournalEntries();
            JournalEntity journalE = ((journalEntities.Count > 0) ? journalEntities.First(j => j.Description == "Create") : null);
            DateTime createdDate = ((journalE != null) ? journalE.CreationDateTime : DateTime.Now);

            PrepaidCardDetailRecord cardDetail = new PrepaidCardDetailRecord()
            {
                CardHolder = new ContactInformation()
                {
                    FirstName = teen.FirstName,
                    MiddleName = teen.MiddleName,
                    LastName = teen.LastName,
                    EmailAddress = teen.EmailAddress,
                    AddressLine1 = teen.Address1,
                    AddressLine2 = teen.Address2,
                    City = teen.City,
                    Country = teen.Country,
                    PhoneNumber = teen.Phone.Number
                },
                PrepaidCardIdentifier = new Identifiers.PrepaidCardAccountIdentifier(prepaidCardAccount.AccountID).DisplayableIdentifier,
                IsPendingReplacement = prepaidCardAccount.IsPendingReplacement,
                UtcCreatedDate = teen.TimeZone.ToUniversalTime(createdDate),
                CardStatus2 = cardStatus2,
                CardStatus = cardStatus2.ToString(),
                CardBalance = balance != null ? balance.Value : 0,
                Ref1 = prepaidCardAccount.AccountID.ToString(),
                CardNumberMasked = prepaidCardAccount.CardNumberMasked,
                CardNumberLastFour = Common.Util.Utils.DisplayLastFourDigits(prepaidCardAccount.CardNumber),
                EmbossName = prepaidCardAccount.EmbossName,
                UtcActivationDate = prepaidCardAccount.ActivateDate,
                ExpirationDate = prepaidCardAccount.ExpirationDate,
            };

            return cardDetail;        
        }
    
        private PrepaidCardStatus2 ConvertToPrepaidCardStatus2(PrepaidCardStatus status)
        {
            switch (status)
            {
                case PrepaidCardStatus.Pending:
                    return PrepaidCardStatus2.PendingActivation;
                case PrepaidCardStatus.Closed:
                    return PrepaidCardStatus2.Closed;
                case PrepaidCardStatus.Good:
                    return PrepaidCardStatus2.Activated;
                case PrepaidCardStatus.Suspended:
                    return PrepaidCardStatus2.Suspended;
                case PrepaidCardStatus.Replaced:
                    return PrepaidCardStatus2.Replaced;
                case PrepaidCardStatus.Unknown:
                    return PrepaidCardStatus2.Unknown;
            }
            return PrepaidCardStatus2.Unknown;
        }
        #endregion
    }
}
