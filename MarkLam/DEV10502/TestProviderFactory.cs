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
using Payjr.DataAdapters.Users;
using Payjr.Entity.EntityClasses;

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

        public IIdentityCheck IdentityCheck { get; set; } //used to test identity check provider
        public IIdentityCheckProvider IdentityCheckProvider { get; set; }
        public ICardDesignsDataAdapter CardDesignsDataAdapterObj{ get; set; }
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
            Error outError;
            string outString;
            RMock.Expect.Call(ppProviderMock.ActivateCard(null, null, out outString, out outError)
                ).IgnoreArguments().Return(true).OutRef(null, null).Repeat.Any();
            RMock.Expect.Call(ppProviderMock.ReturnCounts)
                .Return(1).Repeat.Any();
            RMock.Expect.Call(ppProviderMock.PackagingKey)
                           .Return(Guid.Empty).Repeat.Any();

            RMock.Expect.Call(ppProviderMock.ChargeFee(null, -50, Common.FSV.WebService.FSVFee.MonthlyService, string.Empty, string.Empty, string.Empty, out outString, out outError)).IgnoreArguments()
                           .Return(true).OutRef(string.Empty, null);
            RMock.Expect.Call(ppProviderMock.DeactivateCard(string.Empty, string.Empty, out outString, out outError)).IgnoreArguments().Return(true).OutRef(string.Empty, null);

            RhinoMocks.Replay(ppProviderMock);
            PrepaidCardProvider = ppProviderMock;
        }

       
        public void SetupIdentityCheckProvider(IdentityCheckStatus expectedResponse )
        {
            IIdentityCheckProvider ccProviderMock = MockCreator.CreateMockIIdentityCheckProviderForGetStatusWithValueReturnByStatus(RhinoMocks,expectedResponse);
            RhinoMocks.Replay(ccProviderMock);
            this.IdentityCheckProvider = ccProviderMock;
        }

        public void SetupIdentityCheck(RequestStatusResponse expectedResponse)
        {
            IIdentityCheck providerMock = RhinoMocks.CreateMock<IIdentityCheck>();
            RMock.Expect.Call(
                providerMock.RequestStatus(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<RequestStatusRequest>())).IgnoreArguments().Return(expectedResponse).Repeat.Any();
            RhinoMocks.Replay(providerMock);
            IdentityCheck = providerMock;
        }
        public void SetupCardDesignsDataAdapter(CustomCardDesignUserEntity expectedResponse)
        {
            ICardDesignsDataAdapter ccProviderMock = RhinoMocks.StrictMock<ICardDesignsDataAdapter>();
            RMock.Expect.Call(
                ccProviderMock.RetrieveUserCardDesignFromServerSideID(It.IsAny<string>())).IgnoreArguments().Return(expectedResponse).Repeat.Any();
            RhinoMocks.Replay(ccProviderMock);
            this.CardDesignsDataAdapterObj = ccProviderMock;
        }
        
    }
}

