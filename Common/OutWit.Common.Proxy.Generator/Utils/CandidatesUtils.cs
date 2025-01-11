using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using OutWit.Common.Proxy.Attributes;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class CandidatesUtils
    {
        public static IEnumerable<INamedTypeSymbol> GetCandidates(this Compilation me)
        {
            var allTypes = new List<INamedTypeSymbol>();

            allTypes.AddRange(GetTypesFromAssembly(me.Assembly));

            foreach (MetadataReference referencedAssembly in me.References)
            {
                if (me.GetAssemblyOrModuleSymbol(referencedAssembly) is IAssemblySymbol assemblySymbol)
                    allTypes.AddRange(GetTypesFromAssembly(assemblySymbol));
            }

            return allTypes.Where(symbol => symbol.TypeKind == TypeKind.Interface &&
                                       symbol.GetAttributes().Any(data => data.AttributeClass?.Name == nameof(ProxyTargetAttribute)));
        }

        private static IEnumerable<INamedTypeSymbol> GetTypesFromAssembly(IAssemblySymbol assembly)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(assembly.GlobalNamespace);

            while (stack.Count > 0)
            {
                var namespaceSymbol = stack.Pop();
                foreach (INamespaceOrTypeSymbol member in namespaceSymbol.GetMembers())
                {
                    if (member is INamespaceSymbol namespaceMember)
                        stack.Push(namespaceMember);
                    
                    else if (member is INamedTypeSymbol namedType)
                    {
                        yield return namedType;
                    }
                }
            }
        }

    }
}
