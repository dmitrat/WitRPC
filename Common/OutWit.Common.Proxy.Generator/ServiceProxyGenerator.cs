using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OutWit.Common.Proxy.Attributes;
using OutWit.Common.Proxy.Generator.Generators;
using OutWit.Common.Proxy.Generator.Utils;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Generator
{
    [Generator]
    public class ServiceProxyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (INamedTypeSymbol symbol in context.Compilation.GetCandidates())
            {
                var proxyTargetAttribute = symbol.GetAttributes()
                    .FirstOrDefault(data => data.AttributeClass?.Name == nameof(ProxyTargetAttribute));

                if(proxyTargetAttribute == null)
                    continue;

                var nameArgument = proxyTargetAttribute.ConstructorArguments.FirstOrDefault().Value as string;

                var className = string.IsNullOrEmpty(nameArgument) 
                    ? $"{symbol.Name}Proxy" 
                    : nameArgument;

                GenerateProxy(className!, context, symbol);

            }
        }

        private void GenerateProxy(string className, GeneratorExecutionContext context, INamedTypeSymbol interfaceSymbol)
        {
            var namespaceName = interfaceSymbol.ContainingNamespace.ToDisplayString();

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public class {className} : {interfaceSymbol.ToDisplayString()}");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine($"        private readonly OutWit.Common.Proxy.Interfaces.{nameof(IProxyInterceptor)} _interceptor;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        public {className}(OutWit.Common.Proxy.Interfaces.{nameof(IProxyInterceptor)} interceptor)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            _interceptor = interceptor;");
            sourceBuilder.AppendLine("        }");


            foreach (ISymbol member in interfaceSymbol.GetAllMembers())
            {
                if (member is IMethodSymbol method)
                    method.Generate(sourceBuilder);
                
                else if (member is IPropertySymbol property)
                    property.Generate(sourceBuilder);

                else if (member is IEventSymbol eventSymbol)
                    eventSymbol.Generate(sourceBuilder);
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");


            context.AddSource($"{className}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

    }
}
