using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Proxy.Interfaces
{
    public interface IProxyInterceptor
    {
        public void Intercept(IProxyInvocation invocation);
    }
}
