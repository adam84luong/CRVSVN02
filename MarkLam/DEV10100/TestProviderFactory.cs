using Common.CreditGateway;
using Common.Metrics;
using Common.Service.Providers;
using Moq;
using Payjr.Core.BrandingSite;
using Payjr.Core.Providers;
using Payjr.Core.Services;
using Payjr.Util.Test;
using RMock = Rhino.Mocks;
using System;
using MetricRecorder = Payjr.Core.Metrics.MetricRecorder;
using Payjr.Core.Providers.Interfaces;
using Common.Types;
using Common.Contracts.IdentityCheck;
using Common.Contracts.IdentityCheck.Responses;
using Common.Contracts.Shared.Records;
using Common.Contracts.IdentityCheck.Requests;

namespace Payjr.Core.Test.Providers
{
    public class TestProviderFactory : IProviderFactory
    {
        public ISystemConfigurationProvider SystemConfiguration { get; set; }
        public IMetricProvider Metrics { get; set; }
        public MetricRecorder MetricRecorder { get; set; }
        public RMock.MockRepository RhinoMocks { get; set; }
        public ICreditCardProvider CreditCardProvider { get; set; }
        public ICardProvider PrepaidCardProvider { get; set; }

        private IIdentityCheck _identityCheck; //used to test identity check provider
        private IIdentityCheckProvider _identityCheckProvider;
       
        public int CacheDuration
        {
            get { return 0; }
        }

        public bool ShouldCache
        {
            get { return false; }
        }

        public TestProviderFactory(MockRepository mocks, RMock.MockRepository rhinoMocks = null)
        {
            SystemConfiguration = mocks.Create<ISystemConfigurationProvider>().Object;
            Metrics = mocks.Create<IMetricProvider>().Object;
            MetricRecorder = new TestMetricRecorder(this);
            RhinoMocks = rhinoMocks;
        }

        public IErrorService ErrorService
        {
            get { return new ErrorService(); }
        }

        public ICreditCardProvider CreateCreditCardProvider(Site site)
        {
            if (RhinoMocks == null)
            {
                return site.PrepaidCreditProvider;
            }

            return CreditCardProvider;
        }

        public ICardProvider CreatePrepaidCardProvider(Site site)
        {
            if (RhinoMocks == null)
            {
                return site.PrepaidProvider;
            }

            return PrepaidCardProvider;
        }

        public void SetupCreditCardProvider(Site site, bool wasSuccessful)
        {
            IGatewayReply reply = MockCreator.CreateMockIGatewayReply(RhinoMocks, null, wasSuccessful);
            ICreditCardProvider ccProviderMock = MockCreator.CreateMockICreditCardProvider(RhinoMocks, reply);
            RMock.Expect.Call(ccProviderMock.ProviderID)
                .Return(site.PrepaidModule.CreditProvider.ProviderID).Repeat.Any();
            RhinoMocks.ReplayAll();
            CreditCardProvider = ccProviderMock;
        }

        public void SetupPrepaidCardProvider(Site site, bool wasSuccessful)
        {
            ICardProvider ppProviderMock = MockCreator.CreateMockICardProvider(RhinoMocks, false);
            RMock.Expect.Call(ppProviderMock.ProviderID)
                .Return(site.PrepaidModule.PrepaidProvider.ProviderID).Repeat.Any();
            RhinoMocks.Replay(ppProviderMock);
            PrepaidCardProvider = ppProviderMock;
        }

        public IIdentityCheckProvider CreateIdentityCheckProvider()
        {
            return _identityCheckProvider;
        }

        public void SetupIdentityCheckProvider(IdentityCheckStatus expectedResponse )
        {
            IIdentityCheckProvider ccProviderMock = MockCreator.CreateMockIIdentityCheckProviderForGetStatusWithValueReturnByStatus(RhinoMocks,expectedResponse);
            RhinoMocks.Replay(ccProviderMock);
            _identityCheckProvider = ccProviderMock;
        }

        public IIdentityCheck CreateIdentityCheck()
        {
            return _identityCheck;
        }

        public void SetupIdentityCheck(RequestStatusResponse expectedResponse)
        {
            IIdentityCheck providerMock = RhinoMocks.CreateMock<IIdentityCheck>();
            RMock.Expect.Call(
                providerMock.RequestStatus(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<RequestStatusRequest>())).IgnoreArguments().Return(expectedResponse).Repeat.Any();
            RhinoMocks.Replay(providerMock);
            _identityCheck = providerMock;
        }
        
    }
}
