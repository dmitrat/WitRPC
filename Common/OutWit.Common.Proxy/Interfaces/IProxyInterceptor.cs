using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Proxy.Interfaces
{
    public interface IProxyInterceptor
    {
        public void Intercept(IProxyInvocation invocation);

        public Task<T> InterceptMethodAsync<T>(IProxyInvocation invocation);

        public Task InterceptMethodAsync(IProxyInvocation invocation);

        public object InterceptMethod(IProxyInvocation invocation);

    }
}
