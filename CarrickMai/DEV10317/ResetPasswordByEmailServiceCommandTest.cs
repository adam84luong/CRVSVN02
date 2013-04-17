using System;
using Authentication.Contracts.Authentication;
using Common.Contracts.Shared.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.ServiceCommands;
using Payjr.Core.ServiceCommands.Authentication;
using Payjr.Core.Users;
using System.Collections.Generic;

namespace Payjr.Core.Test.ServiceCommands.Authentication
{
    [TestClass]
    public class ResetPasswordByEmailServiceCommandTest : TestBase
    {
        [TestInitialize]
        public void InitializeTest()
        {
            MyTestInitialize();
        }

        [TestMethod]
        public void ExecuteTestSucess()
        {
            var request = new LocalRequest
                              {
                                  Configuration = new RetrievalConfigurationRecord { ApplicationKey = Guid.NewGuid() },
                                  Email = UserTestBase.EmailAddress
                              };

            var target = new ResetPasswordByEmailServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            string newpassword = result.Data.ToString();       

            Assert.IsTrue(result.Status.IsSuccessful, result.Status.ErrorMessage);
            Assert.IsNotNull(newpassword);
            Assert.AreEqual(result.Result, Result.Success);
        }

        [TestMethod]
        public void ExecuteTestFail()
        {
            var request = new LocalRequest
                              {
                                  Configuration = new RetrievalConfigurationRecord { ApplicationKey = Guid.NewGuid() },
                                  Email = "test@email.com"
                              };

            var target = new ResetPasswordByEmailServiceCommand(ProviderFactory);
            var result = target.Execute(request);

            Assert.IsFalse(result.Status.IsSuccessful, result.Status.ErrorMessage);
            Assert.AreEqual(result.Result, Result.Error);
        }
    }
}
