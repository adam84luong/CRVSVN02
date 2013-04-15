using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands.PrepaidCard;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;
using Common.Contracts.Prepaid.Records;
using Payjr.Core.Identifiers;
using Payjr.Util.Test;
using Common.Types;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Modules;

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardActivationServiceCommandTest : TestBase2
    {
        private string _prepaidCardIdentifier1;
        private string _prepaidCardIdentifier2;
        private PrepaidModule _prepaidModule;
       
        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();

            TestEntityFactory.CreatePrepaidAccount(_teen, false, PrepaidCardStatus.Pending, MockCreator.CreateFakeCreditCardNumber());
            TestEntityFactory.CreatePrepaidAccount(_teen, false, PrepaidCardStatus.Pending, MockCreator.CreateFakeCreditCardNumber());
            
            var ppAcctId1 = _teen.FinancialAccounts.PrepaidCardAccounts[0].AccountID;
            _prepaidCardIdentifier1 = new PrepaidCardAccountIdentifier(ppAcctId1).DisplayableIdentifier;

            var ppAcctId2 = _teen.FinancialAccounts.PrepaidCardAccounts[1].AccountID;
            _prepaidCardIdentifier2 = new PrepaidCardAccountIdentifier(ppAcctId2).DisplayableIdentifier;

            _prepaidModule = TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
        }

        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            CardActivationRequest request = null;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request must not be null", result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_RequestRecordIsEmpty()
        {
            CardActivationRequest request = new CardActivationRequest();
            Assert.AreEqual(0, request.CardActivations.Count);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual("request.CardActivations must not be empty", result.Status.ErrorMessage);
        }
        
        /// <summary>
        /// All of two request records are processed and activated
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_1()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);           
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2, result.CardActivations.Count);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
            Assert.IsTrue(result.CardActivations[1].ActivationSuccessful);
        }

        /// <summary>
        /// All of two request records are processed, just one activated
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_2()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            // set activationData to a value which different with _parent's DOB
            // so that this record will not activate susscessfully
            request.CardActivations[0].ActivationData = DateTime.Now.ToString();
            Assert.AreEqual(2, request.CardActivations.Count);
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2, result.CardActivations.Count);
            Assert.IsFalse(result.CardActivations[0].ActivationSuccessful);
            Assert.IsTrue(result.CardActivations[1].ActivationSuccessful);
        }

        /// <summary>
        /// All of two request records are processed, no one activated
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_3()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            // set activationData to a value which different with _parent's DOB
            // so that this record will not activate susscessfully
            request.CardActivations[0].ActivationData = DateTime.Now.ToString();
            request.CardActivations[1].ActivationData = DateTime.Now.ToString();
            Assert.AreEqual(2, request.CardActivations.Count);
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2, result.CardActivations.Count);
            Assert.IsFalse(result.CardActivations[0].ActivationSuccessful);
            Assert.IsFalse(result.CardActivations[1].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because ActivationData is empty
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_4()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].ActivationData = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because ActivationData is white space
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_5()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].ActivationData = @"  ";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because ActivationData is invalid DateTime value
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_6()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].ActivationData = "InvalidDateTimeValue";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because CardIdentifier is empty
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_7()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].CardIdentifier = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because CardIdentifier is white space
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_8()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].CardIdentifier = "   ";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// One of two request record is not processed activation
        /// because CardIdentifier is invalid format
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_9()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            request.CardActivations[0].CardIdentifier = "InvalidPREPENDEDSTRING";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        /// <summary>
        /// one of two request record is not processed activation
        /// because could not found PrepaidCard with specified CardIdentifier
        /// </summary>
        [TestMethod]
        public void Execute_Successfully_10()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            Assert.AreEqual(2, request.CardActivations.Count);
            var cardId = Guid.NewGuid();
            var cardIdentifier = "PJRPCA:" + cardId;
            request.CardActivations[0].CardIdentifier = cardIdentifier;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }
    
     #region helper methods

        private CardActivationRequest CreateCardActivationRequest(bool initCardActivationRecord)
        {
            var result = new CardActivationRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "CardActivationServiceCommandTest"
                }
            };
            if (initCardActivationRecord)
            {
                //record 1
                result.CardActivations.Add(
                new CardActivationRequestRecord
                {
                    ActivatingUserIdentifier = new UserIdentifier(_parent.UserID).Identifier,
                    ActivationData = _parent.DOB.Value.ToString(),
                    CardIdentifier = _prepaidCardIdentifier1,
                    IPAddress = "127.0.0.1"
                });
                //record 2
                result.CardActivations.Add(
                new CardActivationRequestRecord
                {
                    ActivatingUserIdentifier = new UserIdentifier(_parent.UserID).Identifier,
                    ActivationData = _parent.DOB.Value.ToString(),
                    CardIdentifier = _prepaidCardIdentifier2,
                    IPAddress = "127.0.0.1"
                });
            }
  
            return result;
        }

        #endregion
        }
}
