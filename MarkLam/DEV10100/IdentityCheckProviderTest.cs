using System;
using Common.Contracts.IdentityCheck.Records;
using Common.Contracts.IdentityCheck.Requests;
using Common.Contracts.IdentityCheck.Responses;
using Common.Contracts.Shared.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payjr.Core.Providers;
using Common.Types;
using Payjr.Core.Test.ServiceCommands;

namespace Payjr.Core.Test.Providers
{
    [TestClass()]
    public class IdentityCheckProviderTest : TestBase2
    {
        #region Additional test attributes

        [TestInitialize]
        public void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        #endregion

        #region function testing

        [TestMethod()]
        public void GetStatus_Approved_Successful()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
                                     {
                                         IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                                         Status = IdentityCheckStatus.Approved
                                     });
            ProviderFactory.SetupIdentityCheck(response);
            var target = new IdentityCheckProvider(ProviderFactory,ProviderFactory.IdentityCheck);
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.AreEqual(result,IdentityCheckStatus.Approved);
        }

        [TestMethod()]
        public void GetStatus_Denied_Successful()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
            {
                IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                Status = IdentityCheckStatus.Denied
            });
            ProviderFactory.SetupIdentityCheck(response);
            var target = new IdentityCheckProvider(ProviderFactory, ProviderFactory.IdentityCheck);
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.AreEqual(result, IdentityCheckStatus.Denied);
        }

        [TestMethod()]
        public void GetStatus_Fail_UnkownReturn()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
            {
                IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                Status = IdentityCheckStatus.Unknown
            });
            ProviderFactory.SetupIdentityCheck(response);
            var target = new IdentityCheckProvider(ProviderFactory,ProviderFactory.IdentityCheck);
           
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.AreEqual(result, IdentityCheckStatus.Unknown);
        }
        #endregion
    }
}
