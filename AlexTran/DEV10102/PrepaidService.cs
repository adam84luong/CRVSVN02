using Common.Contracts.Diagnostics.Requests;
using Common.Contracts.Diagnostics.Responses;
using Common.Contracts.Prepaid;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Contracts.Shared.Records;
using Payjr.Core.Providers;
using Payjr.Core.ServiceCommands.Diagnostics;
using Payjr.Core.ServiceCommands.Prepaid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Services
{
	public class PrepaidService : IPrepaid
	{
		private IProviderFactory _providers;

		public PrepaidService()
		{
			_providers = new ProviderFactory();
        }

        #region IPrepaid

        public CardActivationResponse AdminCardActivation(RetrievalConfigurationRecord retrievalConfiguration, CardActivationRequest request)
        {
            throw new NotImplementedException();
        }

        public AuthorizePurchaseResponse AuthorizePurchase(ConfigurationRecord configuration, AuthorizePurchaseRequest request)
        {
            throw new NotImplementedException();
        }

        public CapturePurchaseResponse CapturePurchase(RetrievalConfigurationRecord configuration, CapturePurchaseRequest request)
        {
            throw new NotImplementedException();
        }

        public CardActivationResponse CardActivation(RetrievalConfigurationRecord retrievalConfiguration, CardActivationRequest request)
        {
            //TODO: implement in this sprint
            throw new NotImplementedException();
        }

        public RetrieveTransactionResponse CardTransactionSearch(RetrievalConfigurationRecord retrievalConfiguration, RetrieveTransactionRequest request)
        {
            return new CardTransactionSearchServiceCommand(_providers).Execute(request);
        }

        public void ChangeUserInfo(RetrievalConfigurationRecord retrievalConfiguration, UpdateUserDetailRequest request)
        {
            throw new NotImplementedException();
        }

        public void CloseCard(RetrievalConfigurationRecord retrievalConfiguration, CloseCardRequest request)
        {
            throw new NotImplementedException();
        }

        public CardCreationResponse CreateCard(ConfigurationRecord configuration, CardCreationRequest request)
        {
            throw new NotImplementedException();
        }

        public BinResponse GetBin(RetrievalConfigurationRecord retrievalConfiguration, BinRequest request)
        {
            throw new NotImplementedException();
        }

        public PrepaidCardSearchResponse GetCardDetails(RetrievalConfigurationRecord configuration, PrepaidCardSearchRequest request)
        {
            throw new NotImplementedException();
        }

        public CardStatusResponse GetCardStatus(RetrievalConfigurationRecord retrievalConfiguration, CardStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public LoadStatusResponse GetLoadStatus(RetrievalConfigurationRecord retrievalConfiguration, LoadStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public UnloadStatusResponse GetUnloadStatus(RetrievalConfigurationRecord retrievalConfiguration, UnloadStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public LoadFundsResponse LoadFunds(RetrievalConfigurationRecord retrievalConfiguration, LoadFundsRequest request)
        {
            //TODO: implement in this sprint
            throw new NotImplementedException();
        }

        public void MarkCardAsFraud(RetrievalConfigurationRecord retrievalConfiguration, MarkCardAsFraudRequest request)
        {
            throw new NotImplementedException();
        }

        public void ReplaceCard(RetrievalConfigurationRecord retrievalConfiguration, ReplaceCardRequest request)
        {
            throw new NotImplementedException();
        }
        
        [Obsolete("Use new Search method (TBD)")]
        public RetrieveCardDetailResponse RetrieveCardDetail(RetrievalConfigurationRecord retrievalConfiguration, RetrieveCardDetailRequest request)
        {
            throw new NotImplementedException();
        }

        public RetrieveTransactionResponse RetrieveCardTransactions(RetrievalConfigurationRecord retrievalConfiguration, RetrieveTransactionRequest request)
        {
            throw new NotImplementedException();
        }

        public RetrieveUserDetailResponse RetrieveUserInfo(RetrievalConfigurationRecord retrievalConfiguration, RetrieveUserDetailRequest request)
        {
            throw new NotImplementedException();
        }

        public ReverseAuthorizePurchaseResponse ReverseAuthorizePurchase(ConfigurationRecord configuration, ReverseAuthorizePurchaseRequest request)
        {
            throw new NotImplementedException();
        }

        public void SuspendCard(RetrievalConfigurationRecord retrievalConfiguration, SuspendCardRequest request)
        {
            throw new NotImplementedException();
        }

        public UnloadFundsResponse UnloadFunds(RetrievalConfigurationRecord retrievalConfiguration, UnloadFundsRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Diagnostics

        public RetrieveSoftwareVersionResponse RetrieveSoftwareVersion(RetrieveSoftwareVersionRequest request)
        {
            return new RetrieveSoftwareVersionServiceCommand(_providers.MetricRecorder).Execute(request);
        }

        public RetrieveDatabaseVersionResponse RetrieveDatabaseVersion(RetrieveDatabaseVersionRequest request)
        {
            return new RetrieveDatabaseVersionServiceCommand(_providers).Execute(request);
        }

        public RetrieveApplicationNameResponse RetrieveApplicationName(RetrieveApplicationNameRequest request)
        {
            return new RetrieveApplicationNameServiceCommand(_providers.MetricRecorder).Execute(request);
        }

        public RetrieveHealthStatusResponse RetrieveHealthStatus(RetrieveHealthStatusRequest request)
        {
            return new RetrieveHealthStatusServiceCommand(_providers).Execute(request);
        }

        public RetrieveDeploymentStatusResponse RetrieveDeploymentStatus(RetrieveDeploymentStatusRequest request)
        {
            return new RetrieveDeploymentStatusServiceCommand(_providers).Execute(request);
        }

        #endregion
    }
}
