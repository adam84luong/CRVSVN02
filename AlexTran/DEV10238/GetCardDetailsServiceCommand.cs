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
                throw new ArgumentNullException("request");
            }
            if (request.Configuration == null)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request.Configuration is null");
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (!request.Configuration.ApplicationKey.HasValue)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request.Configuration.ApplicationKey has not value");
                throw new ArgumentException("Configuration.ApplicationKey must be set", "request");
            }
            if (request.Requests.Count == 0)
            {
                Log.Error("GetCardDetailsServiceCommand can not process with request.Requests has not any item");
                throw new ArgumentException("request.Requests must have item", "request");
            }

            cardSearchCriteria = request.Requests;
        }

        protected override bool OnExecute(PrepaidCardSearchResponse response)
        {
            try
            {
                foreach (PrepaidCardSearchCriteria prepaidCardSearch in cardSearchCriteria)
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
                            PrepaidCardStatus2 cardStatus2 = ConvertToPrepaidCardStatus2(prepaidCardAccount.Status);
                            decimal? balance  = prepaidCardAccount.GetBalance();
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
                                PrepaidCardIdentifier = Encoding.ASCII.GetString(prepaidCardAccount.CardIdentifier),
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
                            response.Records.Add(cardDetail);
                        }
                    }
                    else
                    {
                        Log.Info("Error when trying to get card detail by useridentifier: ", prepaidCardSearch.UserIdentifier);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error when trying to get card detail by useridentifier", ex);
                return false;
            }
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
    }
}
