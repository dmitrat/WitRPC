using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using OutWit.Common.Proxy.Utils;
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

            var returnType = proxyInvocation.GetReturnType();

            if (proxyInvocation.IsAsyncGeneric())
            {
                MethodInfo? method = typeof(RequestInterceptor)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x=>x.Name.Equals(nameof(InterceptMethodAsync)))
                    .FirstOrDefault(x=>x.IsGenericMethod);

                if(method == null || !method.IsGenericMethod)
                    return;

                method = method.MakeGenericMethod(returnType.GetGenericArguments().Single());

                invocation.ReturnValue = method.Invoke(this, new[] { proxyInvocation });
            }
            else if (proxyInvocation.IsAsync())
                invocation.ReturnValue = InterceptMethodAsync(proxyInvocation);

            else
            {
                base.Intercept(proxyInvocation);

                invocation.ReturnValue = proxyInvocation.ReturnValue;
            }
           
        }

        #endregion
    }
}
