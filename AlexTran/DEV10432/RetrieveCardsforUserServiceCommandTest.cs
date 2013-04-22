using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.Shared.Records;
using Common.Contracts.CreditCard.Records;
using Payjr.Core.Identifiers;
using Common.Types;
using Payjr.Util.Test;
using Payjr.Entity;
using Payjr.Core.ServiceCommands.CreditCardProcessing;

namespace Payjr.Core.Test.ServiceCommands.CreditCardProcessing
{
    [TestClass]
    public class RetrieveCardsforUserServiceCommandTest : TestBase2
    {
        #region Additional test attributes
        private RetrieveCardRequest _request;
        [TestInitialize]
        public void InitializeTest()
        {
            base.MyTestInitialize();
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            TestEntityFactory.CreateCreditCardAccount(_parent, true, AccountStatus.AllowMoneyMovement);
            _request = CreateRetrieveCardRequest(true);    

        }
        #endregion
        
        #region RetrieveCardsforUser

        [TestMethod]
        public void Execute_Successful()
        {
            _request.RetrieveCardRecords[0].UserIdentifier = new UserIdentifier(_parent.UserID).Identifier;
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.CreditCards.Count);
            var ccAcct = _parent.FinancialAccounts.CreditCardAccounts[0];
            Assert.IsNotNull(ccAcct);
            var ccAcctRecord = result.CreditCards[0];
            Assert.AreEqual("PJRCCA:" + ccAcct.AccountID, ccAcctRecord.AccountIdentifier);
            Assert.AreEqual("0613", ccAcctRecord.CardNumberLastFour);
            Assert.AreEqual(CreditCardType.VISA, ccAcctRecord.CardType);
            Assert.AreEqual(DateTime.Now.ToString("MM"), ccAcctRecord.ExpirationMonth);
            Assert.AreEqual(DateTime.Now.ToString("yyyy"), ccAcctRecord.ExpirationYear);
            Assert.AreEqual("PAYjrUser" + _parent.UserID, ccAcctRecord.UserIdentifier);
        }

        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            _request = null;
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("request must be set{0}Parameter name: request", Environment.NewLine),
                result.Status.ErrorMessage);
        }
       
        [TestMethod]
        public void Execute_Failure_RequestRecordIsEmpty()
        {
            RetrieveCardRequest request = new RetrieveCardRequest();
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "RetrieveCardRecords must be set{0}Parameter name: request.RetrieveCardRecords",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }
       
        [TestMethod]
        public void Execute_Failure_UserIdentifierIsNull()
        {
            _request.RetrieveCardRecords[0].UserIdentifier= string.Empty;
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("UserIdentifier must be set{0}Parameter name: request.RetrieveCardRecords[0].UserIdentifier", Environment.NewLine),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CouldNotFoundCardOwner()
        {

            _request.RetrieveCardRecords[0].UserIdentifier = "UserIdentifier";
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "Could not found an user with user ID = {0}",
                    Guid.Empty),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CardOwnerIsNotParent()
        {

            _request.RetrieveCardRecords[0].UserIdentifier = new UserIdentifier(_teen.UserID).Identifier;
            var target = new RetrieveCardsforUserServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "User ID = {0} has invalid role to retrieve credit card",
                    _teen.UserID),
                result.Status.ErrorMessage);
        }

        #endregion
                
        #region helper methods

        private RetrieveCardRequest CreateRetrieveCardRequest(bool initRetrieveCardRequest)
        {
            var result = new RetrieveCardRequest
            {
                Configuration = new RetrievalConfigurationRecord
                {
                    //ApplicationKey = _branding.BrandingId,
                    //BrandingKey = _branding.BrandingId
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "RetrieveCardRequest"
                }
            };

            if (initRetrieveCardRequest)
            {
                result.RetrieveCardRecords.Add(CreateRetrieveCardRecord());
            }

            return result;
        }    
        private RetrieveCardRecord CreateRetrieveCardRecord()
        {
            var record = new RetrieveCardRecord
            {              
                UserIdentifier = new UserIdentifier(_parent.UserID).Identifier
            };

            return record;
        }

        #endregion
    }
}
