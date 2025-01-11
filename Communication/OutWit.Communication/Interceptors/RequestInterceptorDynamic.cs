using System;
using Castle.DynamicProxy;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Interceptors
{
    public class RequestInterceptorDynamic : RequestInterceptor, IInterceptor
    {
        #region Constructors

        public RequestInterceptorDynamic(IClient client, bool strongAssemblyMatch)
            : base(client, strongAssemblyMatch )
        {
        }

        #endregion

        #region IInterceptor

        public void Intercept(IInvocation invocation)
        {
            var proxyInvocation = invocation.ToProxyInvocation();

            Intercept(proxyInvocation);

            invocation.ReturnValue = proxyInvocation.ReturnValue;
        }

        #endregion
    }
}
