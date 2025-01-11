using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class MembersUtils
    {
        public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol me)
        {
            HashSet<ISymbol> members = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var symbol in me.GetMembers())
                members.Add(symbol);

            foreach (INamedTypeSymbol baseInterface in me.Interfaces)
            {
                foreach (var symbol in baseInterface.GetAllMembers())
                    members.Add(symbol);
            }

            return members;
        }
    }
}
