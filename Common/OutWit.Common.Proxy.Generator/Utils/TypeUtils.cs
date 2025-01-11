using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class TypeUtils
    {
        public static string GetTypeString(this ITypeSymbol me)
        {
            var dd = me as INamedTypeSymbol;
            if (dd == null)
                return "";

            var result = $"{me.ContainingNamespace}.{me.MetadataName}";

            if (dd.TypeArguments.Length >0 )
                result = $"{result}[{string.Join(",", dd.TypeArguments.Select(x => $"[{x.GetTypeString()}]"))}]";


            return $"{result}, {me.ContainingAssembly}";

        }
    }
}
