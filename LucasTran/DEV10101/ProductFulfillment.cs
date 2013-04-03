using Common.Contracts.ProductFulfillment;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Responses;
using Common.Contracts.Shared.Records;
using Payjr.Core.Providers;
using Payjr.Core.ServiceCommands.ProductFulfillment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Services
{
    class ProductFulfillmentService : IProductFulfillment
    {
        private IProviderFactory _providers;

        public ProductFulfillmentService()
		{
			_providers = new ProviderFactory();
        }

        public SendToFulfillmentResponse SendToFulfillment(SendToFulfillmentRequest request)
        {
            return new SendToFulfillmentServiceCommand(_providers).Execute(request);
        }

        public CancelResponse Cancel(RetrievalConfigurationRecord retrievalConfiguration, CancelRequest request)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse Update(RetrievalConfigurationRecord retrievalConfiguration, UpdateRequest request)
        {
            throw new NotImplementedException();
        }
       
        public RetrieveApprovalStatusResponse RetrieveApprovalStatus(RetrievalConfigurationRecord retrievalConfiguration, RetrieveApprovalStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public HoldResponse Hold(RetrievalConfigurationRecord retrievalConfiguration, HoldRequest request)
        {
            throw new NotImplementedException();
        }

        public ResumeResponse Resume(RetrievalConfigurationRecord retrievalConfiguration, ResumeRequest request)
        {
            throw new NotImplementedException();
        }
     
        public ProductFulfillmentDetailResponse RetrieveProductFulfillmentDetail(RetrievalConfigurationRecord retrievalConfiguration, ProductFulfillmentDetailRequest request)
        {
            throw new NotImplementedException();
        }
       
        public ProductFulfillmentOverviewResponse RetrieveFulfillmentOverviewStatus(RetrievalConfigurationRecord retrievalConfiguration, ProductFulfillmentOverviewRequest request)
        {
            throw new NotImplementedException();
        }
       
        public RetrieveFulfillmentStatusInfoResponse RetrieveFulfillmentStatusInformation(RetrievalConfigurationRecord retrievalConfiguration, RetrieveFulfillmentStatusInfoRequest request)
        {
            throw new NotImplementedException();
        }
      
        public AvailableActionResponse RetrieveAvailableAction(RetrievalConfigurationRecord retrievalConfiguration, AvailableActionRequest request)
        {
            throw new NotImplementedException();
        }
       
        public ExecuteActionResponse ExecuteAction(RetrievalConfigurationRecord retrievalConfiguration, ExecuteActionRequest request)
        {
            throw new NotImplementedException();
        }
      
        public RefundResponse Refund(RetrievalConfigurationRecord retrievalConfiguration, RefundRequest request)
        {
            throw new NotImplementedException();
        }
       
        public RetrieveStatusResponse RetrieveStatus(RetrievalConfigurationRecord retrievalConfiguration, RetrieveStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public RetrieveShippingOptionsResponse RetrieveShippingOptions(RetrievalConfigurationRecord retrievalConfiguration, RetrieveShippingOptionsRequest record)
        {
            throw new NotImplementedException();
        }
     
        public PackageProductsForShipmentResponse PackageProductsForShipment(RetrievalConfigurationRecord configuration, PackageProductsForShipmentRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
