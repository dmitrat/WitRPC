using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using OutWit.Common.Proxy;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Common.Proxy.Utils;

namespace OutWit.Communication.Utils
{
    public static class ProxyUtils
    {
        public static IProxyInvocation ToProxyInvocation(this IInvocation me)
        {
            var returnType = me.Method.ReturnType;

            var invocation = new ProxyInvocation
            {
                MethodName = me.Method.Name,
                Parameters = me.Arguments,
                ParametersTypes = me.Method.GetParameters().Select(p => p.ParameterType.TypeString()).ToArray(),
                HasReturnValue = returnType != typeof(void) && returnType != typeof(Task),
                ReturnType = returnType.TypeString(),
            };

            if (returnType == typeof(Task))
                invocation.ReturnsTask = true;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                invocation.ReturnsTaskWithResult = true;

            if (invocation.ReturnsTaskWithResult)
                invocation.TaskResultType = returnType.GenericTypeArguments[0].TypeString();

            if (me.GenericArguments == null || me.GenericArguments.Length == 0)
                invocation.GenericArguments = Array.Empty<string>();
            else
                invocation.GenericArguments = me.GenericArguments.Select(p => p.TypeString()).ToArray();

            return invocation;
        }
    }
}
