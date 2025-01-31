using System;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Utils
{
    public static class ProxyUtils
    {
        public static bool IsAsync(this IProxyInvocation me)
        {
            var returnType = me.GetReturnType();

            if (returnType == typeof(Task))
                return true;

            return false;
        }

        public static bool IsAsyncGeneric(this IProxyInvocation me)
        {
            var returnType = me.GetReturnType();

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return true;

            return false;
        }

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

        public static string TypeString(this Type me)
        {
            return $"{me.AssemblyQualifiedName}";
        }
    }
}
