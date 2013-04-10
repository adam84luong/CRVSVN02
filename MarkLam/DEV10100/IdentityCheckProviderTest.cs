using System;
using Common.Contracts.IdentityCheck.Records;
using Common.Contracts.IdentityCheck.Requests;
using Common.Contracts.IdentityCheck.Responses;
using Common.Contracts.Shared.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payjr.Core.Providers;
using Common.Types;

namespace Payjr.Core.Test.Providers
{
    [TestClass()]
    public class IdentityCheckProviderTest : Payjr.Core.Test.ServiceCommands.TestBase
    {
        #region Additional test attributes

        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        #endregion

        #region function testing

        [TestMethod()]
        public void GetStatusApprovalSuccessfulTest()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
                                     {
                                         IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                                         Status = IdentityCheckStatus.Approved
                                     });
            IdentityCheckMock.Setup(
                mock => mock.RequestStatus(It.IsAny<RetrievalConfigurationRecord>(),
                    It.IsAny<RequestStatusRequest>())).Returns(response);

            var target = new IdentityCheckProvider(ProviderFactory, IdentityCheckMock.Object);
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.Equals(result,IdentityCheckStatus.Approved);
        }

        [TestMethod()]
        public void GetStatusDeniedSuccessfulTest()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
            {
                IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                Status = IdentityCheckStatus.Denied
            });
            IdentityCheckMock.Setup(
                mock => mock.RequestStatus(It.IsAny<RetrievalConfigurationRecord>(),
                    It.IsAny<RequestStatusRequest>())).Returns(response);

            var target = new IdentityCheckProvider(ProviderFactory, IdentityCheckMock.Object);
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.Equals(result, IdentityCheckStatus.Denied);
        }

        [TestMethod()]
        public void GetStatusUnSuccessfulTest()
        {
            var response = new RequestStatusResponse();
            string identityCheckUserIdentifier = "Test";
            response.Records.Add(new RequestStatusResponseRecord
            {
                IdentityCheckUserIdentifier = identityCheckUserIdentifier,
                Status = IdentityCheckStatus.Unknown
            });
            IdentityCheckMock.Setup(
                mock => mock.RequestStatus(It.IsAny<RetrievalConfigurationRecord>(),
                    It.IsAny<RequestStatusRequest>())).Returns(response);

            var target = new IdentityCheckProvider(ProviderFactory, IdentityCheckMock.Object);
            var result = target.GetStatus(new Guid(), identityCheckUserIdentifier);
            Assert.Equals(result, IdentityCheckStatus.Approved);
        }
        #endregion
    }
}
