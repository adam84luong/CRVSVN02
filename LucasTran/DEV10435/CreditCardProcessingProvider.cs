﻿using System;
using CardLab.CMS.Metrics;
using Common.Contracts.CreditCard;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.Shared.Records;
using Common.Service.Providers;
using Common.Types;
using Common.Contracts.CreditCard.Responses;

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

        public bool DeleteAccount(Guid applicationKey, string accountIdentifier)
        {
            var request = new DeleteCardRequest();
            
            request.Requests.Add(new DeleteCardRecord()
            {
                AccountIdentifier = accountIdentifier
            });
            request.Header = new RequestHeaderRecord()
            {
                CallerName = "CardLab.CMS.Providers.CreditCardProcessingProvider.DeleteAccount"
            };
            request.Configuration = new ConfigurationRecord()
            {
                ApplicationKey = applicationKey
            };

            DeleteCardResponse response;
            try
            {
                response = CallService(() => CreateInstance().DeleteCard(applicationKey, request));
            }
            catch (Exception ex)
            {
                Log.ErrorException(String.Format("An error occurred while delete account with accountIdentifier: {0}.", accountIdentifier), ex);
                return false;
            }

            if (!response.Status.IsSuccessful)
            {
                Log.Error(String.Format("An error occurred while delete account with accountIdentifier: {0} . Error: {1}", accountIdentifier, response.Status.ErrorMessage));
                return false;
            }

            if (response.Respones.Count == 0)
            {
                Log.Error(String.Format("An error occurred while delete account because response.Respones is null or Empty"));
                return false;
            }

            if (response.Status.IsSuccessful && response.Respones[0].IsDeleted)
                return true;
            
            return false;
        }
    }
}
