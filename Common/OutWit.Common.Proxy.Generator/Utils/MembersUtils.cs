using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class MembersUtils
    {
        public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol me)
        {
            List<ISymbol> members = me.GetMembers().ToList();

            foreach (INamedTypeSymbol baseInterface in me.Interfaces)
                members.AddRange(GetAllMembers(baseInterface));

            return members;
        }
    }
}
