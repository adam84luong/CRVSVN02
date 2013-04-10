using Common.Contracts.IdentityCheck.Records;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Responses;
using Common.Contracts.Shared.Records;
using Common.Types;
using Payjr.Core.Configuration;
using Payjr.Core.Providers;
using Payjr.Core.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Payjr.Core.ServiceCommands.ProductFulfillment
{
    public class RetrieveApprovalStatusServiceCommand : ProviderServiceCommandBase<RetrieveApprovalStatusRequest, RetrieveApprovalStatusResponse>
    {
        //private IIdentityCheckProvider _identityCheckProvider;
        List<RetrieveApprovalStatusRecord> _listOfRetrieveApprovalStatusRecord;
        RetrievalConfigurationRecord _configuration;
     
        private IMediaServiceProvider mediaServiceProvider;
        private IIdentityCheckProvider identityCheckProvider;
        public RetrieveApprovalStatusServiceCommand(IProviderFactory providers) : base(providers)
        {
           
        }

        public RetrieveApprovalStatusServiceCommand(IIdentityCheckProvider identityCheckerProvider,IMediaServiceProvider mediaServiceProvider, IProviderFactory providers) : base(providers)
        { 
            this.identityCheckProvider = identityCheckerProvider;
            this.mediaServiceProvider = mediaServiceProvider;
        }

        #region Properties
        public IMediaServiceProvider MediaServiceProvider
        {
            get
            {
                return mediaServiceProvider;
            }
            set
            {
                this.mediaServiceProvider = value;
            }
        }
        public IIdentityCheckProvider IdentityCheckProvider
        {
            get
            {
                return identityCheckProvider;
            }
            set
            {
                this.identityCheckProvider = value;
            }
        }
        #endregion
        protected override bool OnExecute(RetrieveApprovalStatusResponse response)
        {
            RetrieveApprovalStatusResponse result = new RetrieveApprovalStatusResponse() ;
          
            foreach (var reqRecord in _listOfRetrieveApprovalStatusRecord)
            {
                List<ProductApprovalFulfillmentRecord> listProductApproval = new List<ProductApprovalFulfillmentRecord>(); 
                foreach (var lineItem in reqRecord.LineItems)
                {
                    ProcessRetrieveApprovalStatus(lineItem.ProductRecords, out listProductApproval);
                    if (listProductApproval.Count > 0)
                    {
                        RetrieveApprovalStatusRecord retrieveApprovalStatusRecord = new RetrieveApprovalStatusRecord();
                        ProductFulfillmentLineItem productFulfilmentLineItem = new  ProductFulfillmentLineItem
                        {
                            Configuration = lineItem.Configuration,
                            LineItemIdentifier =lineItem.LineItemIdentifier,
                        };
                        this.UpdateProductApprovalFromProducctApproval(productFulfilmentLineItem.ProductRecords,listProductApproval);
                        retrieveApprovalStatusRecord.LineItems.Add(productFulfilmentLineItem);
                    }
                }
            }
            result.Status = new ResponseStatusRecord
            {
                ErrorMessage = string.Empty,
                IsSuccessful = true
            };

            return true;
        }

        protected override void Validate(RetrieveApprovalStatusRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }
            ValidateForRequestRecords(request.RequestRecords);
            _listOfRetrieveApprovalStatusRecord = request.RequestRecords;
                        
        }

        #region Validate
        private static void ValidateForRequestRecords(List<RetrieveApprovalStatusRecord> holdRequestRecords)
        {
            if (holdRequestRecords.Count == 0)
            {
                throw new ArgumentException("RequestRecords must be set a value", "request.RequestRecords");
            }

            foreach (var requestRecord in holdRequestRecords)
            {
                if (string.IsNullOrWhiteSpace(requestRecord.Ref1))
                {
                    throw new ArgumentNullException("request.Ref1", "Ref1 must be set a value");
                }
                //ignore Ref2--request.Ref2 is not require 

                var holdLineItems = requestRecord.LineItems;
                if (holdLineItems.Count == 0)
                {
                    throw new ArgumentNullException("request.LineItems", "Line items must be set value");
                }
                foreach (var holdLineItem in holdLineItems)
                {
                    CheckLineItem(holdLineItem);
                }

            }
        }

        private static void CheckLineItem(ProductFulfillmentLineItem holdLineItem)
        {
            if (string.IsNullOrWhiteSpace(holdLineItem.LineItemIdentifier))
            {
                throw new ArgumentNullException("LineItems.LineItemIdentifier", "Line item identifier must be set a value");
            }
            //check Configuration
            if (holdLineItem.Configuration == null)
            {
                throw new ArgumentNullException("LineItems.Configuration", "Configuration must be set a value");
            }

            var config = holdLineItem.Configuration;
            if (config.ApplicationKey == null || config.ApplicationKey == Guid.Empty)
            {
                throw new ArgumentNullException("LineItems.Configuration.ApplicationKey", "Application key must be set a value");
            }

            if (config.SystemConfiguration == null || config.SystemConfiguration == Guid.Empty)
            {
                throw new ArgumentNullException("LineItems.Configuration.SystemConfiguration", "System Configuration must be set a value");
            }
            if (config.TenantKey == null || config.TenantKey == Guid.Empty)
            {
                throw new ArgumentNullException("LineItems.Configuration.TenantKey", "Tenant Key must be set a value");
            }
            if (config.TenantKey == null || config.ApplicationKey == Guid.Empty)
            {
                throw new ArgumentNullException("LineItems.Configuration", "Configuration must be set a value");
            }
            //---------------------------------------------------------
            //check  ProductRecords
            var holdProductRecords = holdLineItem.ProductRecords;
            if (holdProductRecords.Count == 0)
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords", "Product record must be set value");
            }

            foreach (var productRecord in holdProductRecords)
            {
                CheckProductCode(productRecord);
            }
        }

        private static void CheckProductCode(UpdateProductFulfillmentRecord productRecord)
        {
            if (string.IsNullOrWhiteSpace(productRecord.LineItemIdentifier))
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.LineItemIdentifier", "LineItem Identifier record must be set value");
            }
            if (string.IsNullOrWhiteSpace(productRecord.ProductName))
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.ProductName", "Product name must be set value");
            }
            if (string.IsNullOrWhiteSpace(productRecord.ProductCode))
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.ProductCode", "Product code must be set value");
            }
            if (productRecord.Quantity <= 0)
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.Quantity", "Quantity must be positive number");
            }
            if (string.IsNullOrWhiteSpace(productRecord.ValueData))
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.ValueData", "Value data must be set value");
            }
        }
        #endregion

        #region Process

        public void ProcessRetrieveApprovalStatus(List<UpdateProductFulfillmentRecord> products, out List<ProductApprovalFulfillmentRecord> processedProducts)
        {
            processedProducts = new List<ProductApprovalFulfillmentRecord>();
            
            string identityCode = String.IsNullOrEmpty(SystemConfiguration.IdentityCheckProductCode)? null: SystemConfiguration.IdentityCheckProductCode.ToUpper();
            string productCode = String.IsNullOrEmpty(SystemConfiguration.IdentityCheckProductCode) ? null : SystemConfiguration.IdentityCheckProductCode.ToUpper(); 

            foreach (var product in products)
            {
                string productCodeTarget = product.ProductCode;

                // Card Create Product Code
                if (productCode.Equals(productCodeTarget, StringComparison.OrdinalIgnoreCase))
                {
                    processedProducts.Add(GetApprovalStatus(product, mediaServiceProvider));
                }
                // Identity Check Product Code
                else if (String.Equals(identityCode, productCodeTarget))
                {
                    processedProducts.Add(CheckIdentity(product));
                }
                else
                {
                    processedProducts.Add(ProcessApprovalStatusForSingleProduct(product));
                }
            }
        }

        private ProductApprovalFulfillmentRecord GetApprovalStatus(UpdateProductFulfillmentRecord product, IMediaServiceProvider mediaServiceProvider)
        {
            ServerSideImageApprovalStatus statusResp = new ServerSideImageApprovalStatus();
            string errorMessage = string.Empty;
            string denialReason = string.Empty;
            if (mediaServiceProvider.RetrieveApprovalStatus(product.Value, this._configuration.ApplicationKey.Value, ref statusResp, ref denialReason,ref errorMessage))
            {
                return ProcessApprovalStatusForSingleProduct(product, statusResp, denialReason, errorMessage);
            }
            else
            {
                Log.Error("We got back an unexpected error from Media so we are going to throw an exception for line item {0} with value {1}", product.LineItemIdentifier, product.Value);
                throw new Exception("Error getting Approval Status for approval for SKU = " + product.Value);
            }
        }
       
        #endregion

        #region Helper Methods
        private void UpdateProductApprovalFromProducctApproval(List<UpdateProductFulfillmentRecord> productRecords, List<ProductApprovalFulfillmentRecord> productApprRecords)
        {
            foreach (var pa in productApprRecords)
            {
                productRecords.Add(new UpdateProductFulfillmentRecord
                {
                    IsPrimary = pa.IsPrimary,
                    LineItemIdentifier = pa.LineItemIdentifier,
                    Price = pa.Price,
                    ProductCode = pa.ProductCode,
                    ProductName = pa.ProductName,
                    Quantity = pa.Quantity,
                    Shipping = pa.Shipping,
                    Value = pa.Value,
                    ValueData = pa.ValueData
                });
            }
        }
       
        private ProductApprovalFulfillmentRecord CheckIdentity(UpdateProductFulfillmentRecord product)
        {
            Log.Info("Processing approval for line item {0} with value ", product.LineItemIdentifier, product.Value);
            IdentityCheckStatus status = identityCheckProvider.GetStatus(this._configuration.ApplicationKey.Value, product.Value);
            // the status never null
            return ProcessApprovalStatusForSingleProduct(product, status, null, null);
            
        }

        private ProductApprovalFulfillmentRecord ProcessApprovalStatusForSingleProduct(UpdateProductFulfillmentRecord product)
        {
            Log.Info("Processing approval for single product " + product.Value);
            return new ProductApprovalFulfillmentRecord
            {
                ApprovalDenialReason = string.Empty,
                ErrorReason = string.Empty,
                IsApproved = true,
                IsPrimary = product.IsPrimary,
                LineItemIdentifier = product.LineItemIdentifier,
                Price = product.Price,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Quantity = product.Quantity,
                Shipping = product.Shipping,
                Value = product.Value,
                ValueData = product.ValueData
            };
        }

        private ProductApprovalFulfillmentRecord ProcessApprovalStatusForSingleProduct(UpdateProductFulfillmentRecord product,string approvalDenialReason,string errorReason)
        {
            ProductApprovalFulfillmentRecord record = ProcessApprovalStatusForSingleProduct(product);
            record.ApprovalDenialReason = approvalDenialReason;
            record.ErrorReason = errorReason;
            return record;
        }

        private ProductApprovalFulfillmentRecord ProcessApprovalStatusForSingleProduct(UpdateProductFulfillmentRecord product,ServerSideImageApprovalStatus statusResp,string approvalDenialReason,string errorReason)
        {
            ProductApprovalFulfillmentRecord record = ProcessApprovalStatusForSingleProduct(product, approvalDenialReason, errorReason);

            if (statusResp == ServerSideImageApprovalStatus.Approved)
            {
                record.IsApproved = true;
            }
            else if (statusResp == ServerSideImageApprovalStatus.Pending)
            {
                record.IsApproved = null;
            }
            else if (statusResp == ServerSideImageApprovalStatus.Rejected)
            {
                record.IsApproved = false;
            }

            return record;
        }

        private ProductApprovalFulfillmentRecord ProcessApprovalStatusForSingleProduct(UpdateProductFulfillmentRecord product,IdentityCheckStatus statusResp,string approvalDenialReason,string errorReason)
        {
            ProductApprovalFulfillmentRecord record = ProcessApprovalStatusForSingleProduct(product, approvalDenialReason, errorReason);

            switch (statusResp)
            {
                case IdentityCheckStatus.Approved:
                    record.IsApproved = true;
                    break;
                case IdentityCheckStatus.Denied:
                    record.IsApproved = false;
                    break;
                case IdentityCheckStatus.Unknown:
                default:
                    record.IsApproved = null;
                    break;
            }

            return record;
        }
        #endregion

    }
}
