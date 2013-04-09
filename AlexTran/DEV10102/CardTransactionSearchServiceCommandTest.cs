using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands.Prepaid;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;
using Payjr.Util.Test;
using Payjr.Core.Users;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.DatabaseSpecific;
using Common.Types;
using Payjr.Entity;
using Payjr.Core.FinancialAccounts;

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardTransactionSearchServiceCommandTest : TestBase2
    {
        private RetrieveTransactionRequest _request;
        [TestInitialize]
        public void InitializeTest()
        {
            TestEntityFactory.ClearAll();
            Parent parent;
            Teen teen;
            _branding = TestEntityFactory.CreateBranding("Giftcardlab");
            ThemeEntity theme = TestEntityFactory.CreateTheme("Buxx");
            CultureEntity culture = TestEntityFactory.CreateCulture("Culture");
            TestEntityFactory.CreateTeen(_branding, theme, culture, out parent, out teen, true);
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            TestEntityFactory.CreatePrepaidAccount(teen, true, PrepaidCardStatus.Good);
            TestEntityFactory.CreateTransactionLookup("1102", "Short", "Long", true, 0, true);
            TestEntityFactory.CreateTransactionLookup("1103", "Short", "Long", true, 0, true);
            TestEntityFactory.CreateTransactionLookup("1105", "Short", "Long", true, 0, true);
            TestEntityFactory.CreateTransactionLookup("9999", "Short", "Long", true, 0, false);

            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1102", "1", "1231", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1103", "1", "1232", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1105", "1", "1233", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "9999", "1", "1234", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1102", "2", "1235", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1102", "2", "1236", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1103", "3", "1237", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1105", "3", "1238", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1102", "4", "1239", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1103", "4", "12310", DateTime.Today);
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1103", "5", "12311", DateTime.Today.AddDays(2));
            TestEntityFactory.CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1103", "6", "12312", DateTime.Today.AddDays(-2));
      
            _request = CreateRetrieveTransactionRequest(true);
            _request.CardIdentifier = "PJRPCA:" + teen.FinancialAccounts.ActivePrepaidCardAccount.AccountID.ToString().ToUpper();
      
        }
        [TestMethod]
        public void Execute_Successful_NoPaging()
        {
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(5, result.CardTransactions.Count);        
        }
          [TestMethod]
        public void Execute_Successful_paging()
        {
            _request.PageNumber = 2;
            _request.NumberPerPage = 2;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2, result.CardTransactions.Count);
        }
        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            _request = null;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("request must be set{0}Parameter name: request", Environment.NewLine),
                result.Status.ErrorMessage);
        }
     
        [TestMethod]
        public void Execute_Failure_PrepaidCardAccountIsNull()
        {
            _request.CardIdentifier = "PJRPCA:12345678-1234-1234-1234-123456123456";
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
             string.Format(
                 "Could not found a CardTransaction with CardIdentifier = {0}",
                 _request.CardIdentifier),
             result.Status.ErrorMessage);        
        }

        [TestMethod]
        public void Execute_Failure_PageNumberIsLessThanZero()
        {
            _request.PageNumber = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(5, result.CardTransactions.Count);                          
        }
                    
        [TestMethod]
        public void Execute_Failure_NumberPerPageIsLessThanZero()
        {
            _request.NumberPerPage = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(5, result.CardTransactions.Count);                 
        }

        [TestMethod]
        public void Execute_Failure_CompareDate()
        {
            _request.StartDate = DateTime.Today.AddDays(1);
            _request.EndDate = DateTime.Today.AddDays(-1);
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
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
                    ApplicationKey = Guid.NewGuid()
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "CardTransactionSearchServiceCommandTest"
                }
            };

            if (initRetrieveTransactionRequest)
            {
                result.CardIdentifier = "PJRPCA:12345678-1234-1234-1234-123456123456";
                result.StartDate = DateTime.Today.AddDays(-1);
                result.EndDate = DateTime.Today.AddDays(1);
                result.NumberPerPage = 0;
                result.PageNumber = 0;
            }

            return result;
        }         
    

     
        #endregion
      
    }
}
