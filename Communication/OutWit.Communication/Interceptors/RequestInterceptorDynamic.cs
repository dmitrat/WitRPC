using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Interceptors
{
    public class RequestInterceptorDynamic : RequestInterceptor, IInterceptor
    {
        #region Constructors

        public RequestInterceptorDynamic(IClient client, bool allowThreadBlock,  bool strongAssemblyMatch)
            : base(client, allowThreadBlock, strongAssemblyMatch)
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
