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

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardActivationServiceCommandTest : TestBase2
    {
        private Guid _prepaidAccountID;
        private string _prepaidCardIdentifier;
       
        [TestInitialize]
        public void InitializeTest()
        {
            TestEntityFactory.CreatePrepaidAccount(_teen, false, PrepaidCardStatus.Pending);
            _prepaidAccountID = _teen.FinancialAccounts.PrepaidCardAccounts[0].AccountID;
            _prepaidCardIdentifier = new PrepaidCardAccountIdentifier(_prepaidAccountID).DisplayableIdentifier;
        }      

        [TestMethod]
        public void Execute_Successful()
        {
            CardActivationRequest request = CreateCardActivationRequest(true);
            var target = new CardActivationServiceCommand(ProviderFactory);
            var result = target.Execute(request);           
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
             
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
