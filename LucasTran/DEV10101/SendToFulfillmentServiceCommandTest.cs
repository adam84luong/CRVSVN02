using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Contracts.ProductFulfillment.Requests;
using Payjr.Core.ServiceCommands.ProductFulfillment;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.Shared.Records;
using System.Collections.Generic;
using Common.Contracts.OrderProcessing.Records;

namespace Payjr.Core.Test.ServiceCommands.ProductFulfillment
{
    [TestClass]
    public class SendToFulfillmentServiceCommandTest : TestBase2
    {
        
        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            SendToFulfillmentRequest request = null;
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request must be set", result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Successful()
        {
            SendToFulfillmentRequest request = CreateSendToFulfillmentRequest();
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(request.RequestRecords.Count, result.ResponseRecords.Count);
        }

        [TestMethod]
        public void Execute_Failure_RequestRecordsIsEmpty()
        {
            SendToFulfillmentRequest request = new SendToFulfillmentRequest();

            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);

            Assert.AreEqual(0, result.ResponseRecords.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request.RequestRecords must not be null or empty",result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_ProductLineItemsIsEmpty()
        {
            SendToFulfillmentRequest request = CreateSendToFulfillmentRequest_ProductLineItemsIsEmpty();
                   
            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);

            Assert.AreNotEqual(0, request.RequestRecords.Count);

            int i = 0;
            foreach (SendToFulfillmentRecord record in request.RequestRecords)
            {
                Assert.AreEqual(0, record.ProductLineItems.Count);
                Assert.IsNotNull(result.Status);
                Assert.IsFalse(result.Status.IsSuccessful);
                Assert.AreEqual(string.Format("request.RequestRecords[{0}].ProductLineItems must not be null or empty",i),result.Status.ErrorMessage);
                i++;
            }
        }
        [TestMethod]
        public void Execute_Failure_UserIdentifierIsNullOrWhiteSpace()
        {
            SendToFulfillmentRequest request = CreateSendToFulfillmentRequest_UserIdentifierIsNullOrWhiteSpace();

            var target = new SendToFulfillmentServiceCommand(ProviderFactory);
            var result = target.Execute(request);

            Assert.AreNotEqual(0, request.RequestRecords.Count);

            int i = 0;
            foreach (SendToFulfillmentRecord record in request.RequestRecords)
            {
                Assert.AreNotEqual(0, record.ProductLineItems.Count);
                Assert.IsNotNull(result.Status);
                Assert.IsFalse(result.Status.IsSuccessful);
                Assert.AreEqual(string.Format("request.RequestRecords[{0}].UserIdentifier must be set", i), result.Status.ErrorMessage);
                i++; 
            }
        }

        #region helper methods

        private SendToFulfillmentRequest CreateSendToFulfillmentRequest()
        {
            var result = new SendToFulfillmentRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "SendToFulfillmentServiceCommandTest"
                }
            };

            SendToFulfillmentRecord record = new SendToFulfillmentRecord();
            record.CustomerType = "";
            record.ProductLineItems.Add(new ProductFulfillmentLineItem());
            record.Ref1 = "";
            record.ShipmentPackaging = new List<ShipmentPackaging>();
            record.TransactionRecords = new List<Common.Contracts.ProductFulfillment.Records.TransactionRecord>();
            record.UserIdentifier = "PAYjrUser" + _teen.UserID.ToString();
            result.RequestRecords.Add(record);
           
            return result;
        }

        private SendToFulfillmentRequest CreateSendToFulfillmentRequest_ProductLineItemsIsEmpty()
        {
            var result = new SendToFulfillmentRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "SendToFulfillmentServiceCommandTest"
                }
            };
            
            SendToFulfillmentRecord record = new SendToFulfillmentRecord();
            record.CustomerType = "";
            //record.ProductLineItems // IsEmpty
            record.Ref1 = "";
            record.ShipmentPackaging = new List<ShipmentPackaging>();
            record.TransactionRecords = new List<Common.Contracts.ProductFulfillment.Records.TransactionRecord>();
            record.UserIdentifier = "PAYjrUserCD8E5067-8ABB-43FB-9D66-40F961B79F6E";
            result.RequestRecords.Add(record);
            
            return result;
        }

        private SendToFulfillmentRequest CreateSendToFulfillmentRequest_UserIdentifierIsNullOrWhiteSpace()
        {
            var result = new SendToFulfillmentRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "SendToFulfillmentServiceCommandTest"
                }
            };

            SendToFulfillmentRecord record = new SendToFulfillmentRecord();
            record.CustomerType = "";
            record.ProductLineItems.Add(new ProductFulfillmentLineItem());
            record.Ref1 = "";
            record.ShipmentPackaging = new List<ShipmentPackaging>();
            record.TransactionRecords = new List<Common.Contracts.ProductFulfillment.Records.TransactionRecord>();
            record.UserIdentifier = "";
            result.RequestRecords.Add(record);
           
            return result;
        }
        #endregion
    }
}
