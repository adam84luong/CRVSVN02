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

namespace Payjr.Core.ServiceCommands.Prepaid
{
    public class GetCardDetailsServiceCommand : ProviderServiceCommandBase<PrepaidCardSearchRequest, PrepaidCardSearchResponse>
    {
        private PrepaidCardSearchCriteria _cardSearchCriteria;
        private ICardProvider _cardProvider;   
        private Teen _teen;
        private Parent _parent;
      
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
                throw new ArgumentNullException("request", "request must be set");
            }
            if (request.Requests.Count == 0)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request.Requests has not any item");
                throw new ArgumentException("request.Requests must have item", "request");
            }

            _cardSearchCriteria = request.Requests[0];
        }

        protected override bool OnExecute(PrepaidCardSearchResponse response)
        {
            try
            {
                FinancialAccountList<PrepaidCardAccount> account = GetPrepaidCardAccounts();
                foreach (PrepaidCardAccount prepaidCardAccount in account)
                {
                    _parent = User.RetrieveUser(_teen.ParentID) as Parent;
                    PrepaidCardDetailRecord cardDetail = ConvertPrepaidCardAccountToPrepaidCardDetailRecord(prepaidCardAccount, _teen, _parent);
                    response.Records.Add(cardDetail);                  
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

        private FinancialAccountList<PrepaidCardAccount> GetPrepaidCardAccounts()
        {
            if (!string.IsNullOrWhiteSpace(_cardSearchCriteria.PrepaidCardIdentifier))
            {
                return GetPrepaidCardAccountsByPrepaidCardIdentifier(_cardSearchCriteria.PrepaidCardIdentifier);
            }
            else if (!string.IsNullOrWhiteSpace(_cardSearchCriteria.UserIdentifier))
            {               
                if (!string.IsNullOrWhiteSpace(_cardSearchCriteria.CardNumberFull))
                {
                    return GetPrepaidCardAccountsByUserIdentifierAndCardNumberFull(_cardSearchCriteria.UserIdentifier, _cardSearchCriteria.CardNumberFull);
                }
                else
                {
                    return GetPrepaidCardAccountsByUserIdentifier(_cardSearchCriteria.UserIdentifier);
                }
            }
            else if (!string.IsNullOrWhiteSpace(_cardSearchCriteria.CardNumberFull))
            {
                return GetPrepaidCardAccountsByCardNumberFull(_cardSearchCriteria.CardNumberFull);
            }          
            return new FinancialAccountList<PrepaidCardAccount>(new List<PrepaidCardAccount>());
        }

        private FinancialAccountList<PrepaidCardAccount> GetPrepaidCardAccountsByPrepaidCardIdentifier(string prepaidCardIdentifier)
        {
            Guid user = new Identifiers.PrepaidCardAccountIdentifier(prepaidCardIdentifier).PersistableID;
            _teen = User.RetrieveUserByPrepaidCardAccountID(user) as Teen;
            FinancialAccountList<PrepaidCardAccount> result = new FinancialAccountList<PrepaidCardAccount>(new List<PrepaidCardAccount>());
            if (_teen != null)
            {
                if (CardProvider != null)
                {
                    _teen.CardProvider = CardProvider;
                  
                }
                var aPrepaidCard = _teen.FinancialAccounts.GetPrepaidCardAccountByPrepaidCardAccountID(user);
                result.AddItem(aPrepaidCard);              
            }
            return result;
        }

        private FinancialAccountList<PrepaidCardAccount> GetPrepaidCardAccountsByUserIdentifier(string userIdentifier)
        {   
            Guid userId = new Identifiers.UserIdentifier(userIdentifier).ID;
            _teen = User.RetrieveUser(userId) as Teen;
            if (_teen != null)
            {
                if (CardProvider != null)
                {
                    _teen.CardProvider = CardProvider;
                }
                return _teen.FinancialAccounts.PrepaidCardAccounts;
            }
            return new FinancialAccountList<PrepaidCardAccount>(new List<PrepaidCardAccount>());
        }

        private FinancialAccountList<PrepaidCardAccount> GetPrepaidCardAccountsByUserIdentifierAndCardNumberFull(string userIdentifier, string cardNumber)
        {
            Guid userId = new Identifiers.UserIdentifier(userIdentifier).ID;
            _teen = User.RetrieveUser(userId) as Teen;
            FinancialAccountList<PrepaidCardAccount> result = new FinancialAccountList<PrepaidCardAccount>(new List<PrepaidCardAccount>());
            if (_teen != null)
            {
                if (CardProvider != null)
                {
                    _teen.CardProvider = CardProvider;
                }
                var aPrepaidCard = _teen.FinancialAccounts.GetPrepaidCardAccountByCardNumber(cardNumber);
                result.AddItem(aPrepaidCard);
            }
            return result;
        }

        private FinancialAccountList<PrepaidCardAccount> GetPrepaidCardAccountsByCardNumberFull(string cardNumber)
        {
             _teen =User.RetrieveUserByPrepaidCardNumber(cardNumber) as Teen ;
            FinancialAccountList<PrepaidCardAccount> result = new FinancialAccountList<PrepaidCardAccount>(new List<PrepaidCardAccount>());
            if (_teen != null)
            {
                if (CardProvider != null)
                    {
                   _teen.CardProvider = CardProvider;
                 }
                var aPrepaidCard = _teen.FinancialAccounts.GetPrepaidCardAccountByCardNumber(cardNumber);
                result.AddItem(aPrepaidCard);
            }
            return result;
        }
    
        private PrepaidCardDetailRecord ConvertPrepaidCardAccountToPrepaidCardDetailRecord(PrepaidCardAccount prepaidCardAccount, Teen teen, Parent parent)
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
                Purchaser = new ContactInformation()
                {
                    FirstName = parent.FirstName,
                    MiddleName = parent.MiddleName,
                    LastName = parent.LastName,
                    EmailAddress = parent.EmailAddress,
                    AddressLine1 = parent.Address1,
                    AddressLine2 = parent.Address2,
                    City = parent.City,
                    Country = parent.Country,
                    PhoneNumber = parent.Phone.Number
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

