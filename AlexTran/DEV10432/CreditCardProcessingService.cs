using Common.Contracts.CreditCard;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.CreditCard.Responses;
using Common.Contracts.Diagnostics.Requests;
using Common.Contracts.Diagnostics.Responses;
using Payjr.Core.Providers;
using Payjr.Core.ServiceCommands.CreditCardProcessing;
using Payjr.Core.ServiceCommands.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Services
{
	public class CreditCardProcessingService : ICreditCardProcessing
	{
		private IProviderFactory _providers;

		public CreditCardProcessingService()
		{
			_providers = new ProviderFactory();
		}

        #region ICreditCardProcessing

        public AddCardResponse AddCard(Guid applicationKey, AddCardRequest request)
        {
            return new AddCardServiceCommand(_providers).Execute(request);
        }
        
        public AuthorizeResponse Authorize(Guid applicationKey, AuthorizeRequest request)
        {
            //TODO: implement in this sprint
            throw new NotImplementedException();
        }
        
        public CancelTransactionResponse CancelTransaction(Guid applicationKey, CancelTransactionRequest request)
        {
            throw new NotImplementedException();
        }
        
        public DeleteCardResponse DeleteCard(Guid applicationKey, DeleteCardRequest request)
        {
            throw new NotImplementedException();
        }
        
        public RefundResponse Refund(Guid applicationKey, RefundRequest request)
        {
            throw new NotImplementedException();
        }
        
        public RetrieveCardResponse RetrieveCardsforUser(Guid applicationKey, RetrieveCardRequest request)
        {
            throw new NotImplementedException();
        }
        
        public DeviceFingerprintInfoResponse RetrieveDeviceFingerprintInfo(Guid applicationKey)
        {
            throw new NotImplementedException();
        }

        public TransactionStatusResponse RetrieveTransactionStatus(Guid applicationKey, TransactionStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public StandAloneCreditResponse StandAloneCredit(Guid applicationKey, StandAloneCreditRequest request)
        {
            throw new NotImplementedException();
        }

        public UpdateAuthorizedTransactionResponse UpdateAuthorizedTransaction(Guid applicationKey, UpdateAuthorizedTransactionRequest request)
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
