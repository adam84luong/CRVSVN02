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
             _request = CreateRetrieveTransactionRequest(true);
        }
        [TestMethod]
        public void Execute_Successful()
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
            CreateCardTransaction(teen.FinancialAccounts.ActivePrepaidCardAccount, "1102", "1", "123", DateTime.Today);
           
            _request.CardIdentifier = "PJRPCA:" + teen.FinancialAccounts.ActivePrepaidCardAccount.AccountID.ToString().ToUpper();
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.CardTransactions.Count);        
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
        public void Execute_Failure_PageNumberIsZero()
        {
            _request.PageNumber = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
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
            _request.NumberPerPage = -1;
            var target = new CardTransactionSearchServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "NumberPerPage must >=0{0}Parameter name: request.NumberPerPage",
                    Environment.NewLine),
                result.Status.ErrorMessage);
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
     
        private void CreateCardTransaction(PrepaidCardAccount account, string transactionType, string ref1, string merchantRef, DateTime transactionDate)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                CardTransactionEntity cardTrans = new CardTransactionEntity();

                //Go get the purse
                cardTrans.PurseId = null;
                cardTrans.PrepaidAccountId = account.AccountID;
                cardTrans.Amount = 2.00M;
                cardTrans.Fee = 0.00M;
                cardTrans.TransactionType = transactionType;              
                cardTrans.TransactionDate = transactionDate;
                cardTrans.TransactionEntryDate = transactionDate;
                cardTrans.RunningBalance = 0.00M;             
                cardTrans.TranId = "123";
                cardTrans.Ref1 = ref1;
                cardTrans.Ref2 = "";
                cardTrans.Mmc = "1234";
                cardTrans.MerchantId = "ID";
                cardTrans.TerminalId = "1234";
                cardTrans.MerchantRef = merchantRef;
                cardTrans.MerchantNameAddress = "NAMEADDRESS";
                cardTrans.PrepaidCardNumber = "1234567891234";
                cardTrans.PrepaidCardNumber = account.CardNumber;
                cardTrans.PrepaidCardNumberLastFour = account.CardNumber.Substring(account.CardNumber.Length - 4, 4);
                // vcReference;
                //FiservID;
                if (!adapter.SaveEntity(cardTrans))
                {
                }
            }
        }

     
        #endregion
      
    }
}
