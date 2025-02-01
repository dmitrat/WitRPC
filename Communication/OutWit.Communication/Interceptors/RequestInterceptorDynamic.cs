using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using OutWit.Common.Proxy.Utils;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Interceptors
{
    public class RequestInterceptorDynamic : RequestInterceptor, IInterceptor
    {
        #region Constructors

        public RequestInterceptorDynamic(IClient client, bool strongAssemblyMatch)
            : base(client, strongAssemblyMatch)
        {

        }

        #endregion

        #region IInterceptor

        public void Intercept(IInvocation invocation)
        {
            var proxyInvocation = invocation.ToProxyInvocation();

            var returnType = proxyInvocation.GetReturnType();

            base.Intercept(proxyInvocation);

            if (proxyInvocation.ReturnsTaskWithResult)
            {
                MethodInfo? method = typeof(RequestInterceptorDynamic)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.Name.Equals(nameof(CastTask)))
                    .FirstOrDefault(x => x.IsGenericMethod);

                if (method == null || !method.IsGenericMethod)
                    return;

                method = method.MakeGenericMethod(returnType.GetGenericArguments().Single());

                invocation.ReturnValue = method.Invoke(this, new[] { proxyInvocation.ReturnValue });
            }

            else
                invocation.ReturnValue = proxyInvocation.ReturnValue;
        }

        #endregion

        #region Functions

        private Task<T> CastTask<T>(Task<object?> task)
        {
            return task.ContinueWith(x => (T)x.Result);
        }

        #endregion
    }
}
