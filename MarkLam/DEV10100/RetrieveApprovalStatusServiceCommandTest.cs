using System;
using Common.Business;
using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Test.Providers;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using Payjr.Entity;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Entity.EntityClasses;
using Payjr.Core.ServiceCommands.ProductFulfillment;
using Payjr.Util.Test;
using Payjr.Core.FSV;
using Common.FSV.WebService;
using Payjr.Entity.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity.FactoryClasses;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Records;
using System.Collections.Generic;
using Payjr.Core.Providers.Interfaces;
using Payjr.Core.Configuration;


namespace Payjr.Core.Test.ServiceCommands.ProductFulfillment
{
    [TestClass]
    public class RetrieveApprovalStatusServiceCommandTest : TestBase2
    {
        [TestInitialize]
        public void InitializeTest()
        {
            base.MyTestInitialize();
        }

        [TestMethod]
        public void Execute_IdentityCheck_Sucessful_Approved()
        {
            RetrieveApprovalStatusRequest _request = CreateRetrieveApprovalStatusRequest(true);
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Approved;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);

            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.ResponseRecords.Count);
            foreach (ProductApprovalFulfillmentRecord item in result.ResponseRecords[0].ProductRecords)
            {
                if (item.ProductCode == SystemConfiguration.IdentityCheckProductCode)
                    Assert.IsTrue(item.IsApproved.Value);
            }
        }


        [TestMethod]
        public void Execute_IdentityCheck_Sucessful_Denied()
        {
            RetrieveApprovalStatusRequest _request = CreateRetrieveApprovalStatusRequest(true);
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Denied;
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);

            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.ResponseRecords.Count);
            foreach (ProductApprovalFulfillmentRecord item in result.ResponseRecords[0].ProductRecords)
            {
                if (item.ProductCode == SystemConfiguration.IdentityCheckProductCode)
                   Assert.IsFalse(item.IsApproved.Value);
            }
           
        }

        [TestMethod]
        public void Execute_IdentityCheck_Sucessful_Unkown()
        {
            RetrieveApprovalStatusRequest _request = CreateRetrieveApprovalStatusRequest(true);
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Unknown;
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);

            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.ResponseRecords.Count);
            foreach (ProductApprovalFulfillmentRecord item in result.ResponseRecords[0].ProductRecords)
            {
                if (item.ProductCode == SystemConfiguration.IdentityCheckProductCode)
                    Assert.IsNull(item.IsApproved);
            }
            
        }

        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            RetrieveApprovalStatusRequest request = null;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.ResponseRecords.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request must be set value\r\nParameter name: request", result.Status.ErrorMessage);
        }
        [TestMethod]
        public void Execute_Failure_RequestWithoutRecord()
        {
            RetrieveApprovalStatusRequest request = new RetrieveApprovalStatusRequest();
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.ResponseRecords.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("Request records must be set a value\r\nParameter name: request.RequestRecords", result.Status.ErrorMessage);
        }
        [TestMethod]
        public void Execute_Failure_RequestWithoutProductCodeValue()
        {
            RetrieveApprovalStatusRequest request = CreateRetrieveApprovalStatusRequest(true);
            request.RequestRecords[0].LineItems[0].ProductRecords[2].ProductCode = string.Empty;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Approved;
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            int count = CountIdentityCheckSuccess(result);
            Assert.AreEqual(0, count);
        }
        [TestMethod]
        public void Execute_Failure_IsEmptyApplicationKeyInLineItem()
        {
            RetrieveApprovalStatusRequest request = CreateRetrieveApprovalStatusRequest(true);
            request.RequestRecords[0].LineItems[0].Configuration.ApplicationKey = Guid.Empty;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(0, result.ResponseRecords.Count);
        }

        [TestMethod]
        public void Execute_IdentityCheck_Sucessful_With2ApprovalStatusRecord_2Approved()
        {
            RetrieveApprovalStatusRequest _request = CreateRetrieveApprovalStatusRequest(true);
            //add 1 more RetrieveApprovalStatusRecord
            RetrieveApprovalStatusRecord approvalRecord = new RetrieveApprovalStatusRecord();
            approvalRecord.Ref1 = "Ref2";
            approvalRecord.LineItems.AddRange(GetProductFulfilmentLineItems());
            _request.RequestRecords.Add(approvalRecord);
            //----------------------
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Approved;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);

            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2, result.ResponseRecords.Count);
            int count = CountIdentityCheckSuccess(result);
            Assert.AreEqual(2, count);
        }

       
        [TestMethod]
        public void Execute_IdentityCheck_Sucessful_With2ApprovalStatusRecord_1Approved()
        {
            RetrieveApprovalStatusRequest _request = CreateRetrieveApprovalStatusRequest(true);
            //add 1 more RetrieveApprovalStatusRecord
            RetrieveApprovalStatusRecord approvalRecord = new RetrieveApprovalStatusRecord();
            approvalRecord.Ref1 = "Ref2";
            approvalRecord.LineItems.AddRange(GetProductFulfilmentLineItems());
            _request.RequestRecords.Add(approvalRecord);
            _request.RequestRecords[1].LineItems[0].Configuration.ApplicationKey = Guid.Empty;
            //----------------------
            IdentityCheckStatus expectedRespone = IdentityCheckStatus.Approved;
            var target = new RetrieveApprovalStatusServiceCommand(ProviderFactory);
            ProviderFactory.SetupIdentityCheckProvider(expectedRespone);

            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.ResponseRecords.Count);
            int count = CountIdentityCheckSuccess(result);
            Assert.AreEqual(1, count);
        }
        #region Helper

        private static int CountIdentityCheckSuccess(Common.Contracts.ProductFulfillment.Responses.RetrieveApprovalStatusResponse result)
        {
            int count = 0;
            foreach (var rasrItem in result.ResponseRecords)
            {
                foreach (ProductApprovalFulfillmentRecord item in rasrItem.ProductRecords)
                {
                    if (item.ProductCode == SystemConfiguration.IdentityCheckProductCode && item.IsApproved == true)
                        count++;
                }
            }
            return count;
        }
        private RetrieveApprovalStatusRequest CreateRetrieveApprovalStatusRequest(bool isInit)
        {
            var result = new RetrieveApprovalStatusRequest
            {
                Configuration = new RetrievalConfigurationRecord
                {
                    ApplicationKey = Guid.NewGuid()
                    //BrandingKey = _branding.BrandingId
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "RetrieveApprovalStatusServiceCommandTest",
                }
            };
            if (isInit)
            {
                RetrieveApprovalStatusRecord approvalRecord = new RetrieveApprovalStatusRecord();
                approvalRecord.Ref1 = "Ref1";
                approvalRecord.LineItems.AddRange(GetProductFulfilmentLineItems());
                result.RequestRecords.Add(approvalRecord);
            }
            return result;
        }

        private List<ProductFulfillmentLineItem> GetProductFulfilmentLineItems()
        {
            List<ProductFulfillmentLineItem> result  = new List<ProductFulfillmentLineItem>();
            result.Add(new ProductFulfillmentLineItem
            {
                LineItemIdentifier = "LineItemIdentifier1",
                Configuration = new ConfigurationRecord()
                { 
                    ApplicationKey = Guid.NewGuid(),
                    SystemConfiguration = Guid.NewGuid(),
                    TenantKey = Guid.NewGuid()
                }
            });
            result[0].ProductRecords.AddRange(GetUpdateProductFulfillmenetRecords());
            return result;
        }
        private List<UpdateProductFulfillmentRecord> GetUpdateProductFulfillmenetRecords()
        {
            List<UpdateProductFulfillmentRecord> result = new List<UpdateProductFulfillmentRecord>();
            result.Add(new UpdateProductFulfillmentRecord { 
                 IsPrimary = true,
                 LineItemIdentifier = "OrderLineItemIdentifier1",
                 Price = 5,
                 ProductCode = "110000",
                 ProductName = "Buxx Card",
                 Quantity = 1,
                 Shipping = null,
                 Value = "test.jpg",
                 ValueData = ""
            });
            result.Add(new UpdateProductFulfillmentRecord
            {
                IsPrimary = true,
                LineItemIdentifier = "OrderLineItemIdentifier2",
                Price = 5,
                ProductCode = "110001",
                ProductName = "Card Value",
                Quantity = 1,
                Shipping = null,
                Value = "50",
                ValueData = ""
            });
            result.Add(new UpdateProductFulfillmentRecord
            {
                IsPrimary = true,
                LineItemIdentifier = "OrderLineItemIdentifier3",
                Price = 5,
                ProductCode = "110019",
                ProductName = "Identity Check",
                Quantity = 1,
                Shipping = null,
                Value = "88dc99db-b67e-4c77-91c8-68a3a756b9a6",
                ValueData = ""
            });
            return result;
        }
        
        #endregion
    }
}
