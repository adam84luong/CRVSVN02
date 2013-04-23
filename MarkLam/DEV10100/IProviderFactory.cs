using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Service.Providers;
using Payjr.Core.Metrics;
using Payjr.Core.Services;
using Payjr.Core.BrandingSite;
using Payjr.Core.Providers.Interfaces;
using Payjr.DataAdapters.Users;

namespace Payjr.Core.Providers
{
    public interface IProviderFactory : IProviderFactoryBase<MetricRecorder>
    {
        IErrorService ErrorService { get; }
        ICreditCardProvider CreateCreditCardProvider(Site site);
        ICardProvider CreatePrepaidCardProvider(Site site);
        IIdentityCheckProvider IdentityCheckProvider {get;}
        ICardDesignsDataAdapter CardDesignsDataAdapterObj { get; }

    }
}
