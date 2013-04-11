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
        private Guid _prepaidAccountID;
        private string _prepaidCardIdentifier;
        private PrepaidModule _prepaidModule;
       
        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
            TestEntityFactory.CreatePrepaidAccount(_teen, false, PrepaidCardStatus.Pending);
            _prepaidAccountID = _teen.FinancialAccounts.PrepaidCardAccounts[0].AccountID;
            _prepaidCardIdentifier = new PrepaidCardAccountIdentifier(_prepaidAccountID).DisplayableIdentifier;
            _prepaidModule = TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
        }      

        [TestMethod]
        public void Execute_Successful()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);           
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsTrue(result.CardActivations[0].ActivationSuccessful);
        }

        [TestMethod]
        public void Execute_Successful_ActivationIsFailed_ParrentDOBNotMatch()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivationData = DateTime.Now.ToString();
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1, result.CardActivations.Count);
            Assert.IsFalse(result.CardActivations[0].ActivationSuccessful);
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
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations must not be null or empty",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_ActivationDataIsEmpty()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivationData = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations[0].ActivationData must be invalid DateTime value",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_ActivationDataIsWhiteSpaceOnly()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivationData = @"  ";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations[0].ActivationData must be invalid DateTime value",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_ActivationDataIsInvalidDateTimeValue()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivationData = "InvalidDateTimeValue";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations[0].ActivationData must be invalid DateTime value",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CardIdentifierIsEmpty()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].CardIdentifier = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations[0].CardIdentifier must not be null or empty",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CardIdentifierIsWhiteSpaceOnly()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].CardIdentifier = "   ";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "request.CardActivations[0].CardIdentifier must not be null or empty",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CardIdentifierIsInvalidFormat()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].CardIdentifier = "InvalidPREPENDEDSTRING";
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                "The Identifier is not valid - DisplayableIdentifier: InvalidPREPENDEDSTRING",
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_CouldNotFoundPrepaidCard()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            var cardId = Guid.NewGuid();
            var cardIdentifier = "PJRPCA:" + cardId;
            request.CardActivations[0].CardIdentifier = cardIdentifier;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.CardActivations.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                        "Could not found Prepaid Card with ID={0}",
                        cardId),
                result.Status.ErrorMessage);
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
                result.CardActivations.Add(
                new CardActivationRequestRecord
                {
                    ActivatingUserIdentifier = new UserIdentifier(_parent.UserID).Identifier,
                    ActivationData = _parent.DOB.Value.ToString(),
                    CardIdentifier = _prepaidCardIdentifier,
                });
            }
  
            return result;
        }

        #endregion
        }
}
