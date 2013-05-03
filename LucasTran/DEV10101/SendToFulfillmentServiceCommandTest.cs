using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Contracts.ProductFulfillment.Requests;
using Payjr.Core.ServiceCommands.ProductFulfillment;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.Shared.Records;
using System.Collections.Generic;
using Common.Contracts.OrderProcessing.Records;
using Payjr.Util.Test;
using Payjr.Core.Users;
using Common.Types;
using Payjr.Core.Configuration;
using Payjr.Entity.EntityClasses;
using Payjr.Core.Jobs;
using Payjr.Core.UserInfo;
using Payjr.Core.FinancialAccounts;
using IDENTIFIERS = Payjr.Core.Identifiers;

namespace Payjr.Core.Test.ServiceCommands.ProductFulfillment
{
    [TestClass]
    public class SendToFulfillmentServiceCommandTest : TestBase2
    {
        private Teen _teen1;
        private Teen _teen2;
        private string _userIdentifier1;
        private string _userIdentifier2;
        string sku1 = "3m82vyhx2";
        string sku2 = "yy33vyhx2";


        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();

            TestEntityFactory.CreateTeen(_branding, _theme, _culture, _parent, out _teen1);
            TestEntityFactory.CreateTeen(_branding, _theme, _culture, _parent, out _teen2);

            _userIdentifier1 = "PAYjrUser" + _teen1.UserID.ToString();
            _userIdentifier2 = "PAYjrUser" + _teen2.UserID.ToString();
        }

        [TestMethod]
        public void Execute_Successful()
        {
            SendToFulfillmentRequest request = CreateSendToFulfillmentRequest();
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);

            CheckWritedForJob();
            CheckWritedPrepaidCardAccount();
            ChecWritedForCustomCardDesign();

            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);  
        }
       
        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            SendToFulfillmentRequest request = null;
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.ResponseRecords.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request must be set", result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_RequestRecordsIsEmpty()
        {
            SendToFulfillmentRequest request = new SendToFulfillmentRequest();
            Assert.AreEqual(0, request.RequestRecords.Count);
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.ResponseRecords.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request.RequestRecords must not be null or empty",result.Status.ErrorMessage);
        }

        #region helper methods

        private List<UpdateProductFulfillmentRecord> CreateUpdateProductFulfillmenetRecords(string productcodevalue)
        {
            List<UpdateProductFulfillmentRecord> result = new List<UpdateProductFulfillmentRecord>();
            result.Add(new UpdateProductFulfillmentRecord
            {
                IsPrimary = true,
                LineItemIdentifier = "OrderLineItemIdentifier1",
                Price = 5,
                ProductCode = "110000",
                ProductName = "Buxx Card",
                Quantity = 1,
                Shipping = null,
                Value = productcodevalue,
                ValueData = ""
            });
            result.Add(new UpdateProductFulfillmentRecord
            {
                IsPrimary = true,
                LineItemIdentifier = "OrderLineItemIdentifier2",
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

        private List<ProductFulfillmentLineItem> CreateProductLineItems(string productcodevalue)
        {
            List<ProductFulfillmentLineItem> productlineitems = new List<ProductFulfillmentLineItem>();
            ProductFulfillmentLineItem lineitem = new ProductFulfillmentLineItem();
            lineitem.ProductRecords.AddRange(CreateUpdateProductFulfillmenetRecords(productcodevalue));
            productlineitems.Add(lineitem);
            return productlineitems;
        }

        private SendToFulfillmentRequest CreateSendToFulfillmentRequest()
        {
            var result = new SendToFulfillmentRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "SendToFulfillmentServiceCommandTest"
                }
            };

            TestEntityFactory.CreateUserCustomCardDesign(_teen1, sku1);
            
            SendToFulfillmentRecord record1 = new SendToFulfillmentRecord();
            record1.CustomerType = "";
            record1.ProductLineItems.AddRange(CreateProductLineItems(sku1));
            record1.Ref1 = "";
            record1.ShipmentPackaging = new List<ShipmentPackaging>();
            record1.TransactionRecords = new List<Common.Contracts.ProductFulfillment.Records.TransactionRecord>();
            record1.UserIdentifier = _userIdentifier1;
            result.RequestRecords.Add(record1);

            SendToFulfillmentRecord record2 = new SendToFulfillmentRecord();
            record2.CustomerType = "";
            record2.ProductLineItems.AddRange(CreateProductLineItems(sku2));
            record2.Ref1 = "";
            record2.ShipmentPackaging = new List<ShipmentPackaging>();
            record2.TransactionRecords = new List<Common.Contracts.ProductFulfillment.Records.TransactionRecord>();
            record2.UserIdentifier = _userIdentifier2;
            result.RequestRecords.Add(record2);

            return result;
        }

        private void CheckWritedPrepaidCardAccount()
        {
            var card1 = TestEntityFactory.RetrievePrepaidCardAccountsByUser(_teen1.UserID);
            var card2 = TestEntityFactory.RetrievePrepaidCardAccountsByUser(_teen2.UserID);
            Assert.AreEqual(1, card1.Count);
            Assert.AreEqual(1, card2.Count);
        }
        private void ChecWritedForCustomCardDesign()
        {
            CustomCardDesignEntity carddesign1 = CustomCardDesign.RetrieveCardDesignByServerSideId(sku1);
            CustomCardDesignEntity carddesign2 = CustomCardDesign.RetrieveCardDesignByServerSideId(sku2);
            Assert.IsNotNull(carddesign1);
            Assert.IsNotNull(carddesign2);
        }
        private void CheckWritedForJob()
        {
            var njList1 = Job.RetrieveJobs(_teen1.UserID);
            var njList2 = Job.RetrieveJobs(_teen2.UserID);
            Assert.AreEqual(2, njList1.Count);
            Assert.AreEqual(1, njList2.Count);
            bool check = false;
            if ((njList1[0].JobType == Entity.JobType.CreateCardJob || njList1[1].JobType == Entity.JobType.CreateCardJob)
                    && njList2[0].JobType == Entity.JobType.CreateCardJob)
                check = true;
            Assert.IsTrue(check);
        }

        #endregion
    }
}
