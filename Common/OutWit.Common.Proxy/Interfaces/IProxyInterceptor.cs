using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Proxy.Interfaces
{
    public interface IProxyInterceptor
    {
        public void Intercept(IProxyInvocation invocation);
    }
}
