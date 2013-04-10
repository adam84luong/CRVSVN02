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
using Common.Contracts.Media;
using Common.Contracts.Media.Requests;
using Common.Contracts.Media.Records;
using Common.Contracts.Media.Responses;

namespace Payjr.Core.Providers
{
    //author: Mark
    public class MediaServiceProvider : ServiceProviderBase<IMedia, IProviderFactory, MetricRecorder>, IMediaServiceProvider
    {
        public MediaServiceProvider(IMedia mediaObj, IProviderFactory providerObj)
            : base(providerObj, mediaObj)
        {

        }
        public  bool RetrieveApprovalStatus(string value, Guid applicationKey, ref ServerSideImageApprovalStatus serverSideImgApprStatus, ref string denialReason,ref string errorMessage)
        {
            StatusRequest request = new StatusRequest();
            request.Records.Add(
                new StatusRequestRecord()
                {
                   CardID = value
                });

            IMedia service = CreateInstance();
            StatusResponse response = service.RetrieveStatus(applicationKey, request);

            if (response != null && response.Status.IsSuccessful && response.Records.Count > 0)
            {
                StatusResponseRecord responseRecord = response.Records[0]; 
                if (responseRecord != null)
                {
                    serverSideImgApprStatus = responseRecord.Status;
                    denialReason = responseRecord.RejectionReason;
                    return true;
                }
            }
            if (response != null)
            {
                errorMessage = response.Status.ErrorMessage;
                return true;

            }
            return false ;
        }

    }
}
