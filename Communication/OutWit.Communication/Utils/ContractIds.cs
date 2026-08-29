using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OutWit.Communication.Utils
{
    /// <summary>
    /// Deterministic identifiers for contracts and their methods, computed
    /// identically on both ends from the contract's shape alone —
    /// namespace-qualified names with no assembly identity — so a client can
    /// address a method by id and the server can dispatch with one dictionary
    /// lookup, deserializing parameters against the method's declared types
    /// instead of resolving type names sent over the wire.
    /// <para>
    /// Generic methods keep the name-based path: their closed signatures differ
    /// per call, so they carry <see cref="NONE"/> and resolve as before.
    /// </para>
    /// </summary>
    public static class ContractIds
    {
        #region Constants

        public const long NONE = 0;

        private const ulong FNV_OFFSET = 14695981039346656037UL;

        private const ulong FNV_PRIME = 1099511628211UL;

        #endregion

        #region Functions

        public static long GetContractId(Type contract)
        {
            return Hash(StableName(contract));
        }

        public static long GetMethodId(Type contract, MethodInfo method)
        {
            if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                return NONE;

            return GetMethodId(contract, method.Name,
                method.GetParameters().Select(info => info.ParameterType).ToArray());
        }

        public static long GetMethodId(Type contract, string methodName, Type[] parameterTypes)
        {
            var builder = new StringBuilder(StableName(contract));

            builder.Append('#').Append(methodName).Append('(');
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append(StableName(parameterTypes[i]));
            }
            builder.Append(')');

            return Hash(builder.ToString());
        }

        /// <summary>
        /// Namespace-qualified name with generic arguments rendered recursively
        /// and no assembly identity, so the same contract type produces the same
        /// name on every runtime and version.
        /// </summary>
        public static string StableName(Type type)
        {
            if (type.IsArray)
                return StableName(type.GetElementType()!) + "[]";

            if (type.IsGenericParameter)
                return "!" + type.GenericParameterPosition;

            if (type.IsGenericType)
            {
                string definition = type.GetGenericTypeDefinition().FullName!;

                int tick = definition.IndexOf('`');
                string name = tick >= 0 ? definition.Substring(0, tick) : definition;

                return name + "<" + string.Join(",", type.GetGenericArguments().Select(StableName)) + ">";
            }

            return type.FullName ?? type.Name;
        }

        #endregion

        #region Tools

        /// <summary>FNV-1a over the UTF-16 code units, 64 bit.</summary>
        private static long Hash(string value)
        {
            unchecked
            {
                ulong hash = FNV_OFFSET;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= FNV_PRIME;
                }

                return (long)hash;
            }
        }

        #endregion
    }
}
