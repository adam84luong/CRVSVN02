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
        List<RetrieveApprovalStatusRecord> _listOfRetrieveApprovalStatusRecord;
        RetrievalConfigurationRecord _configuration;
     
        public RetrieveApprovalStatusServiceCommand(IProviderFactory providers) : base(providers)
        {
           
        }

        #region Properties
       
        #endregion
        protected override bool OnExecute(RetrieveApprovalStatusResponse response)
        {
            //RetrieveApprovalStatusResponse result = new RetrieveApprovalStatusResponse() ;
          
            foreach (var reqRecord in _listOfRetrieveApprovalStatusRecord)
            {
                List<ProductApprovalFulfillmentRecord> listProductApproval = new List<ProductApprovalFulfillmentRecord>(); 
                foreach (var lineItem in reqRecord.LineItems)
                {
                    ProcessRetrieveApprovalStatus(lineItem.Configuration.ApplicationKey, lineItem.ProductRecords, out listProductApproval);
                    if (listProductApproval.Count > 0)
                    {
                        RetrieveApprovalStatusResponseRecord retrieveApprovalStatusRecord = new RetrieveApprovalStatusResponseRecord();
                        retrieveApprovalStatusRecord.ProductRecords.AddRange(listProductApproval);
                        response.ResponseRecords.Add(retrieveApprovalStatusRecord);
                    }
                }
            }
            response.Status = new ResponseStatusRecord
            {
                ErrorMessage = string.Empty,
            };
            return true;
        }

        protected override void Validate(RetrieveApprovalStatusRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set value");
            }
            ValidateForRequestRecords(request.RequestRecords);
            _listOfRetrieveApprovalStatusRecord = request.RequestRecords;
            _configuration = request.Configuration;
                        
        }

        #region Validate
        private static void ValidateForRequestRecords(List<RetrieveApprovalStatusRecord> holdRequestRecords)
        {
            if (holdRequestRecords.Count == 0)
            {
                throw new ArgumentException("Request records must be set a value", "request.RequestRecords");
            }

            foreach (var requestRecord in holdRequestRecords)
            {
                if (string.IsNullOrWhiteSpace(requestRecord.Ref1))
                {
                    throw new ArgumentNullException("request.RequestRecords.Ref1", "Ref1 must be set a value");
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
                throw new ArgumentNullException("request.Line+Items.ProductRecords.ProductName", "Product name must be set value");
            }
            if (string.IsNullOrWhiteSpace(productRecord.ProductCode))
            {
                throw new ArgumentNullException("request.LineItems.ProductRecords.ProductCode", "Product code must be set value");
            }
            //if (productRecord.Quantity <= 0)
            //{
            //    throw new ArgumentNullException("request.LineItems.ProductRecords.Quantity", "Quantity must be positive number");
            //}
            //if (string.IsNullOrWhiteSpace(productRecord.ValueData))
            //{
            //    throw new ArgumentNullException("request.LineItems.ProductRecords.ValueData", "Value data must be set value");
            //}
        }
        #endregion

        #region Process

        public void ProcessRetrieveApprovalStatus(Guid applicationKey, List<UpdateProductFulfillmentRecord> products, out List<ProductApprovalFulfillmentRecord> processedProducts)
        {
            processedProducts = new List<ProductApprovalFulfillmentRecord>();
            
            string identityCode = String.IsNullOrEmpty(SystemConfiguration.IdentityCheckProductCode)? null: SystemConfiguration.IdentityCheckProductCode;
            string productCode = String.IsNullOrEmpty(SystemConfiguration.CardProductCode) ? null : SystemConfiguration.CardProductCode; 
            //IMediaServiceProvider mediaServiceProvider = this.Providers.c
            foreach (var product in products)
            {
                string productCodeTarget = product.ProductCode;
                // Card Create Product Code
                if (String.Equals(productCode,productCodeTarget, StringComparison.OrdinalIgnoreCase))
                {
                    //processedProducts.Add(GetApprovalStatus(product, mediaServiceProvider));
                    ///TODO:
                }
                // Identity Check Product Code
                else if (String.Equals(identityCode, productCodeTarget,StringComparison.OrdinalIgnoreCase))
                {
                    processedProducts.Add(CheckIdentity(applicationKey, product));
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

        private ProductApprovalFulfillmentRecord CheckIdentity(Guid applicationKey, UpdateProductFulfillmentRecord product)
        {
            Log.Info("Processing approval for line item {0} with value ", product.LineItemIdentifier, product.Value);
            var identityCheckProvider = Providers.CreateIdentityCheckProvider();
            IdentityCheckStatus status = identityCheckProvider.GetStatus(applicationKey, product.Value);
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
