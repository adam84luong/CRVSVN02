using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Service.Providers;
using Payjr.Core.Metrics;
using Payjr.Core.Services;
using Payjr.Core.BrandingSite;

namespace Payjr.Core.Providers
{
    public interface IProviderFactory : IProviderFactoryBase<MetricRecorder>
    {
        IErrorService ErrorService { get; }
        ICreditCardProvider CreateCreditCardProvider(Site site);
        ICardProvider CreatePrepaidCardProvider(Site site);
    }
}
