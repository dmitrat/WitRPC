using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using OutWit.Common.Proxy.Generator.Utils;

namespace OutWit.Common.Proxy.Generator.Generators
{
    public static class MethodGenerator
    {
        public static void Generate(this IMethodSymbol me, StringBuilder sourceBuilder)
        {
            if(me.AssociatedSymbol is IPropertySymbol)
                return;

            if(me.AssociatedSymbol is IEventSymbol)
                return;

            var returnType = me.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", me.Parameters.Select(symbol => $"{symbol.Type} {symbol.Name}"));
            var parameterNames = string.Join(", ", me.Parameters.Select(symbol => symbol.Name));
            var parameterTypes = string.Join(", ", me.Parameters.Select(symbol => $"\"{symbol.Type.GetTypeString()}\""));

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        public {returnType} {me.Name}({parameters})");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine($"            var invocation = new OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                MethodName = \"{me.Name}\",");
            sourceBuilder.AppendLine($"                Parameters = new object[] {{ {parameterNames} }},");
            sourceBuilder.AppendLine($"                ParameterTypes = new string[] {{ {parameterTypes} }},");
            sourceBuilder.AppendLine($"                ReturnType = \"{me.ReturnType.GetTypeString()}\"");
            sourceBuilder.AppendLine("            };");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("            _interceptor.Intercept(invocation);");
            sourceBuilder.AppendLine();

            if (returnType != "void")
            {
                sourceBuilder.AppendLine($"            if (invocation.ReturnValue != null)");
                sourceBuilder.AppendLine($"                return ({returnType})invocation.ReturnValue;");
                sourceBuilder.AppendLine($"            return default({returnType});");
            }

            sourceBuilder.AppendLine("        }");
        }
    }
}
