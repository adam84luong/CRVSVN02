using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Service.Providers;
using Payjr.Core.Metrics;
using Payjr.Core.Services;
using System.Diagnostics;
using Payjr.Core.BrandingSite;
using Payjr.Core.Providers.Interfaces;

namespace Payjr.Core.Providers
{
    public class ProviderFactory : ProviderFactoryBase<MetricRecorder>, IProviderFactory
    {
        public override int CacheDuration
        {
            get { return 0; }
        }

        public override bool ShouldCache
        {
            get { return false; }
        }

        public override MetricRecorder MetricRecorder
        {
            get { return new MetricRecorder(Metrics); }
        }

        public IErrorService ErrorService
        {
            [DebuggerStepThrough()]
            get
            {
                return new ErrorService();
            }
        }

        public ICreditCardProvider CreateCreditCardProvider(Site site)
        {
            return site.PrepaidCreditProvider;
        }

        public ICardProvider CreatePrepaidCardProvider(Site site)
        {
            return site.PrepaidProvider;
        }

        public IIdentityCheckProvider CreateIdentityCheckProvider()
        {
            return new IdentityCheckProvider(this);
        }
    }
}
