using System;

namespace OutWit.Common.Proxy.Interfaces
{
    public interface IProxyInterceptor
    {
        public void Intercept(IProxyInvocation invocation);

    }
}
