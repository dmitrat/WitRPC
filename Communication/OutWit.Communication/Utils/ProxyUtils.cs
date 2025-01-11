using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using OutWit.Common.Proxy;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Communication.Utils
{
    public static class ProxyUtils
    {
        public static Type[] GetParametersTypes(this IProxyInvocation me)
        {
            if (me.ParametersTypes == null || me.ParametersTypes.Length == 0)
                return Array.Empty<Type>();

            return me.ParametersTypes.Select(Type.GetType).ToArray()!;
        }

        public static Type[] GetGenericArguments(this IProxyInvocation me)
        {
            if (me.GenericArguments == null || me.GenericArguments.Length == 0)
                return Array.Empty<Type>();

            return me.GenericArguments.Select(Type.GetType).ToArray()!;
        }

        public static Type GetReturnType(this IProxyInvocation me)
        {
            if(string.IsNullOrEmpty(me.ReturnType))
                return typeof(void);

            try
            {
                return Type.GetType(me.ReturnType) ?? typeof(void);
            }
            catch (Exception e)
            {
                return typeof(void);
            }
        }

        public static IProxyInvocation ToProxyInvocation(this IInvocation me)
        {
            var invocation = new ProxyInvocation
            {
                MethodName = me.Method.Name,
                Parameters = me.Arguments,
                ParametersTypes = me.Method.GetParameters().Select(p => p.ParameterType.TypeString()).ToArray(),
                ReturnType = me.Method.ReturnType.TypeString()
            };
            if(me.GenericArguments == null || me.GenericArguments.Length == 0)
                invocation.GenericArguments = Array.Empty<string>();
            else
                invocation.GenericArguments = me.GenericArguments.Select(p => p.TypeString()).ToArray();

            return invocation;
        }

        public static string TypeString(this Type me)
        {
            return $"{me.AssemblyQualifiedName}";
        }
    }
}
