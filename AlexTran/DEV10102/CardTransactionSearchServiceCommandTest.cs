using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands.Prepaid;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardTransactionSearchServiceCommandTest : TestBase2
    {
        [TestMethod]
        public void Execute_Successful()
        {
            var request = CreateRetrieveTransactionRequest(true);            
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.CardTransactions.Count);       
        }

        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            RetrieveTransactionRequest request = null;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("request must be set{0}Parameter name: request", Environment.NewLine),
                result.Status.ErrorMessage);
        }
        [TestMethod]
        public void Execute_Failure_CardIdentifierIsEmpty()
        {
            var request = CreateRetrieveTransactionRequest(true);            
            request.CardIdentifier = string.Empty;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "CardIdentifier must be set{0}Parameter name: request.CardIdentifier",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_PageNumberIsZero()
        {
            var request = CreateRetrieveTransactionRequest(true);
            request.PageNumber = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "PageNumber must >=0{0}Parameter name: request.PageNumber",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }
        
        [TestMethod]
        public void Execute_Failure_NumberPerPageIsZero()
        {
            var request = CreateRetrieveTransactionRequest(true);
            request.NumberPerPage = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "PageNumber must >=0{0}Parameter name: request.NumberPerPage",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CompareDate()
        {
            var request = CreateRetrieveTransactionRequest(true);
            request.StartDate = DateTime.Parse("01/01/2013");
            request.EndDate = DateTime.Parse("01/01/2011");
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);         
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "StartDate must earlier than is EndDate{0}Parameter name: request.StartDate, request.EndDate",
                 Environment.NewLine),
                result.Status.ErrorMessage);
        }

        #region helper methods

        private RetrieveTransactionRequest CreateRetrieveTransactionRequest(bool initRetrieveTransactionRequest)
        {
            var result = new RetrieveTransactionRequest
            {
                Configuration = new RetrievalConfigurationRecord
                {
                    //ApplicationKey = _branding.BrandingId,
                    //BrandingKey = _branding.BrandingId
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "CardTransactionSearchServiceCommandTest"
                }
            };

            if (initRetrieveTransactionRequest)
            {
                result.CardIdentifier="123456789";
                result.StartDate = DateTime.Parse("01/01/2010");
                result.EndDate = DateTime.Parse("01/01/2013");
                result.NumberPerPage = 10;
                result.PageNumber = 10;
            }

            return result;
        }

     
        #endregion
      
    }
}
