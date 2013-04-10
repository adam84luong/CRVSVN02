using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands.PrepaidCard;
using Payjr.Core.Providers;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;
using Common.Contracts.Prepaid.Records;
using Payjr.Core.Identifiers;
using Payjr.Util.Test;
using Common.Types;
using Payjr.Core.FinancialAccounts;

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardActivationServiceCommandTest : TestBase2
    {
        private PrepaidCardAccount _prepaidAccountID;
        private CardActivationRequest _request;

        [TestInitialize]
        public void InitializeTest()
        {
            TestEntityFactory.CreatePrepaidAccount(_teen, false, PrepaidCardStatus.Pending);
            var pcaID = _teen.FinancialAccounts.PrepaidCardAccounts[0].AccountID;
            Guid _prepaidAccountID = pcaID;
            _request = CreateCardActivationRequest(true);
           
        }
      // Execute_Successful() chua hoan thanh
        [TestMethod]
        public void Execute_Successful()
        {
            var request = CreateCardActivationRequest(true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(_request);           
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);           
            Assert.IsNotNull(_prepaidAccountID);
            _request.CardActivations[0].CardIdentifier = new PrepaidCardAccountIdentifier(_prepaidAccountID.ToString()).DisplayableIdentifier;          
        }
    /// <summary>
    ///All testcase passed
    /// </summary>
        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            CardActivationRequest request = null;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("request must be set{0}Parameter name: request", Environment.NewLine),
                result.Status.ErrorMessage);
        }
        [TestMethod]
        public void Execute_Failure_RequestRecordIsEmpty()
        {
            CardActivationRequest request = new CardActivationRequest();
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "cardActivationRecords must be set{0}Parameter name: request.cardActivationRecords",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }
        [TestMethod]
        public void Execute_Failure_ActivatingUserIdentifierIsEmpty()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivatingUserIdentifier = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "ActivatingUserIdentifier must be set{0}Parameter name: request.cardActivationRecords[0].ActivatingUserIdentifier",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_ActivationDataIsEmpty()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].ActivationData = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "ActivationData must be set{0}Parameter name: request.cardActivationRecords[0].ActivationData",
                    Environment.NewLine),
                result.Status.ErrorMessage);
        }

        [TestMethod]
        public void Execute_Failure_IPAddressIsEmpty()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            request.CardActivations[0].IPAddress = string.Empty;
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "ipAddress must be set{0}Parameter name: request.cardActivationRecords[0].IPAddress",
                    Environment.NewLine),
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

            result.CardActivations.Add(
            new CardActivationRequestRecord
            {
                ActivatingUserIdentifier =
                new UserIdentifier(_parent.UserID).Identifier,
                ActivationData = _parent.DOB.Value.ToString(),
                CardIdentifier = "PJRPCA:12345678-1234-1234-1234-123456123456" 
            });
  
        return result;
        }

        #endregion
        }
}
