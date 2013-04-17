using System;
using Common.Contracts.IdentityCheck.Records;
using Common.Contracts.Shared.Records;
using Common.Contracts.IdentityCheck.Responses;
using Common.Contracts.IdentityCheck.Requests;
using Common.Logging;
using Common.Service.Providers;
using Common.Contracts.IdentityCheck;
using Payjr.Core.Metrics;
using Payjr.Core.Providers.Interfaces;
using Common.Types;

namespace Payjr.Core.Providers
{
    public class IdentityCheckProvider :  ServiceProviderBase<IIdentityCheck, IProviderFactory, MetricRecorder>,IIdentityCheckProvider
    {


        public IdentityCheckProvider(IProviderFactory providerFactory)
            : base(providerFactory)
        {}

        public IdentityCheckProvider(IProviderFactory providerFactory, string endpointConfigurationName)
            : base(providerFactory, endpointConfigurationName)
        {
        }

        public IdentityCheckProvider(IProviderFactory providerFactory, IIdentityCheck serviceOverride)
            : base(providerFactory, serviceOverride)
        {
        }


        public IdentityCheckStatus GetStatus(Guid applicationKey, string identityCheckIdentifier)
        {
            RequestStatusRequest request = new RequestStatusRequest();
            request.Records.Add(
                new RequestStatusRecord()
                {
                    IdentityCheckUserIdentifier = identityCheckIdentifier
                });

            RetrievalConfigurationRecord configuration = new RetrievalConfigurationRecord();
            configuration.ApplicationKey = applicationKey;
            RequestStatusResponse response = CallService(() => CreateInstance().RequestStatus(configuration, request));
            RequestStatusResponseRecord responseRecord = null;
            if (response != null && response.Records.Count > 0)
                responseRecord = response.Records[0];
            if (responseRecord != null)
            {
                return responseRecord.Status;
            }
            return IdentityCheckStatus.Unknown;
        }

    }
}
