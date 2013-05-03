using System;
using CardLab.CMS.Metrics;
using Common.Contracts.CreditCard;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.Shared.Records;
using Common.Service.Providers;
using Common.Types;
using System.Collections.Generic;

namespace CardLab.CMS.Providers
{
    public class CreditCardProcessingProvider : ServiceProviderBase<ICreditCardProcessing, IProviderFactory, MetricRecorder>, ICreditCardProcessingProvider
    {
        public CreditCardProcessingProvider(IProviderFactory providerFactory, string endpointConfigurationName) : base(providerFactory, endpointConfigurationName)
        {
        }

        public CreditCardProcessingProvider(IProviderFactory providerFactory, ICreditCardProcessing serviceOverride) : base(providerFactory, serviceOverride)
        {
        }

        public CreditCardProcessingProvider(IProviderFactory providerFactory) : base(providerFactory)
        {
        }

        public CreditCardDetailedRecord CreateCreditCard(Guid appicationKey, string userIdentifier, UserDetailRecord userInfo, string cardNumber, string cvv2, int expirationMonth, int expirationYear, CreditCardType cardType)
        {
            var addCardRecord = new AddCardRecord()
            {
                UserIdentifier = userIdentifier,
                User = userInfo,
                CardNumber = cardNumber,
                CVV2 = cvv2,
                ExpirationMonth = expirationMonth.ToString("G"),
                ExpirationYear = expirationYear.ToString("G"),
                CardType = cardType
            };

            var configurationRecord = new ConfigurationRecord()
                                          {
                                              ApplicationKey = appicationKey,
                                              BrandingKey = appicationKey,
                                              SystemConfiguration = Guid.Empty,
                                              TenantKey = Guid.Empty
                                          };

            var request = new AddCardRequest();
            request.AddCardsRecords.Add(addCardRecord);
            request.Configuration = configurationRecord;

            try
            {
                var response = CallService(() => CreateInstance().AddCard(appicationKey, request));

                if (response.CreditCards.Count > 0)
                {
                    return response.CreditCards[0];
                }
            }
            catch (Exception e)
            {
                Log.ErrorException("Error creation credit card", e);
            }
            return null;
        }

        public List<CreditCardDetailedRecord> RetrieveAccounts(Guid appicationKey,string userIdentifier)
        {
            //RetrieveCardResponse RetrieveCardsforUser(Guid applicationKey, RetrieveCardRequest request);
            RetrieveCardRecord retrieveCardRecord = new RetrieveCardRecord
            { 
                UserIdentifier = userIdentifier
            };
            //We call method RetrieveCardsforUser to get result.but the some argurment in this method will be ignored without making affect to expected result.
            RetrieveCardRequest request = new RetrieveCardRequest();
            request.Header = new RequestHeaderRecord
            {
                CallerName = "CardLab.CMS.Providers.CreditCardProcessingProvider.RetrieveAccounts" 
            };
            request.RetrieveCardRecords.Add(retrieveCardRecord);
            List<CreditCardDetailedRecord> creditCardDetailedRecords = new List<CreditCardDetailedRecord>();
            try
            {
                var response = CallService(() => CreateInstance().RetrieveCardsforUser(appicationKey,request));
                if (response.CreditCards.Count > 0)
                {
                    creditCardDetailedRecords.AddRange(response.CreditCards);               
                }
            }
            catch (Exception e)
            {
                Log.ErrorException("Error retrieve account", e);
            }
            return creditCardDetailedRecords;
        }
    }
}
