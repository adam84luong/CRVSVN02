using Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.Providers.Interfaces
{
    public interface IIdentityCheckProvider: IDisposable   
    {
        IdentityCheckStatus GetStatus(Guid applicationKey, string identityCheckIdentifier);

    }
}
