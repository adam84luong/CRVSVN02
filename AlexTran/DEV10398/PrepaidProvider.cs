using System;
using System.Collections.Generic;
using System.Linq;
using CardLab.CMS.Metrics;
using Common.Contracts.Prepaid;
using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Contracts.Shared.Records;
using Common.Service.Providers;


namespace CardLab.CMS.Providers
{
    public class PrepaidProvider : ServiceProviderBase<IPrepaid, IProviderFactory, MetricRecorder>, IPrepaidProvider
    {
		public PrepaidProvider(IProviderFactory factory, string endpointName)
			: base(factory, endpointName)
        {
        }

		public PrepaidProvider(IProviderFactory factory)
			: base(factory)
        {
        }

		public PrepaidProvider(IProviderFactory factory, IPrepaid service)
			: base(factory, service)
        {
        }

        public List<PrepaidCardDetailRecord> RetrieveCardDetaislByUserIdentifier(Guid applicationKey, string userIdentifier)
        {
            var request = new PrepaidCardSearchRequest();
            request.Requests.Add(new PrepaidCardSearchCriteria()
                                     {
                                         UserIdentifier = userIdentifier
                                     });
            var configuration = new RetrievalConfigurationRecord()
                                    {
                                        ApplicationKey = applicationKey
                                    };

            request.Configuration = configuration;
            request.Header = new RequestHeaderRecord()
                                    {
                                        CallerName = "CardLab.CMS.Providers.PrepaidProvider.RetrieveCardDetailsByUserIdentifier"
                                    };

            PrepaidCardSearchResponse response;
            try
            {
                response = CallService(() => CreateInstance().GetCardDetails(configuration, request));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while processing RetrieveCardDetaislByUserIdentifers: " + userIdentifier, ex);
                return null;
            }
            if(response.Status.IsSuccessful)
            {
                return response.Records;
            }
            Log.Error(String.Format("Failure when trying to RetrieveCardDetaislByUserIdentifers usertIdentifier {0}. Error: {1}", userIdentifier, response.Status.ErrorMessage));            
            return null;
        }

        /// <summary>
        /// Allow activation a card
        /// </summary>
        /// <param name="applicationKey"></param>
        /// <param name="cardIdentifier"></param>
        /// <param name="actingUserIdentifier"></param>
        /// <param name="ipAddress"></param>
        /// <param name="activeData"></param>
        /// <returns></returns>
        public bool CardActivation(Guid applicationKey, string cardIdentifier, string actingUserIdentifier, string ipAddress, string activeData)
        {
            var configuration = new RetrievalConfigurationRecord()
            {
                ApplicationKey = applicationKey
            };
            var cardActivationRequestRecord = new CardActivationRequestRecord()
            {
                ActivatingUserIdentifier = actingUserIdentifier,
                CardIdentifier = cardIdentifier,
                IPAddress = ipAddress,
                ActivationData = activeData
            };
            var cardActivationRequest = new CardActivationRequest();
            cardActivationRequest.CardActivations.Add(cardActivationRequestRecord);

            CardActivationResponse cardActivationResponse;
            try
            {
                cardActivationResponse = CallService(() => CreateInstance().CardActivation(configuration, cardActivationRequest));
            }
            catch (Exception ex)
            {
                Log.ErrorException(String.Format("An error occurred while activing CardIdentifier {0} by userIdentifier {1}.", cardIdentifier, actingUserIdentifier), ex);
                return false;
            }

            if (!cardActivationResponse.Status.IsSuccessful)
            {
                Log.Error(String.Format("An error occurred while activing CardIdentifier {0} by userIdentifier {1} . Error: {2}", cardIdentifier, actingUserIdentifier, cardActivationResponse.Status.ErrorMessage));
                return false;
            }

            if (cardActivationResponse.CardActivations.Count > 0)
            {
                CardActivationRecord cardActived = cardActivationResponse.CardActivations.First();
                return cardActived.ActivationSuccessful;
            }
            return false;
        }
        public List<CardTransactionRecord> RetrieveCardTransactions(Guid applicationKey, string cardIdentifier, DateTime startDate, DateTime endDate, int pageNumber, int numberPerPage, out int totalRecord)
        {
            var request = new RetrieveTransactionRequest
            {
                Configuration = new RetrievalConfigurationRecord
                {
                    ApplicationKey = applicationKey
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "CardLab.CMS.Providers.PrepaidProvider.RetrieveCardTransactions"
                }
            };
            request.CardIdentifier = cardIdentifier;
            request.StartDate = startDate;
            request.EndDate = endDate;
            request.NumberPerPage = numberPerPage;
            request.PageNumber = pageNumber;

            RetrieveTransactionResponse response;
            try
            {
                response = CallService(() => CreateInstance().RetrieveCardTransactions(request.Configuration, request));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while processing PrepaidCard with CardIdentifier: " + cardIdentifier, ex);
                totalRecord = 0;
                return null;
            }
            if (response.Status.IsSuccessful)
            {
                totalRecord = response.TotalTransactions;
                return response.CardTransactions;
            }
            Log.Error(String.Format("Failure when trying to RetrieveCardTransactions CardIdentifier {0}. Error: {1}", cardIdentifier, response.Status.ErrorMessage));
            totalRecord = 0;
            return null;
        }
       
    }
}