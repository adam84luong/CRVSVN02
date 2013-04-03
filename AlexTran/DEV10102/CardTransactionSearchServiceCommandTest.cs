using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands.Prepaid;
using Common.Contracts.Prepaid.Requests;

namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class CardTransactionSearchServiceCommandTest : TestBase2
    {
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
    }
}
