using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using OutWit.Common.Proxy.Generator.Utils;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Generator.Generators
{
    public static class MethodGenerator
    {
        public static void Generate(this IMethodSymbol me, StringBuilder sourceBuilder, GeneratorExecutionContext context)
        {
            if(me.AssociatedSymbol is IPropertySymbol)
                return;

            if(me.AssociatedSymbol is IEventSymbol)
                return;

            var returnType = me.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", me.Parameters.Select(symbol => $"{symbol.Type} {symbol.Name}"));
            var parameterNames = string.Join(", ", me.Parameters.Select(symbol => symbol.Name));
            var parameterTypes = string.Join(", ", me.Parameters.Select(symbol => $"\"{symbol.Type.GetTypeString()}\""));
            
            bool returnsTask = me.ReturnType.ToString().StartsWith("System.Threading.Tasks.Task");
            bool returnsTaskWithResult = returnsTask && me.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1;
            var taskResultType = returnsTaskWithResult 
                ? ((INamedTypeSymbol)me.ReturnType).TypeArguments[0]
                : null;

            bool hasReturnValue = !SymbolEqualityComparer.Default.Equals(me.ReturnType, context.Compilation.GetSpecialType(SpecialType.System_Void)) && !returnsTask;

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        public {returnType} {me.Name}({parameters})");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine($"            var invocation = new OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.MethodName)} = \"{me.Name}\",");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.Parameters)} = new object[] {{ {parameterNames} }},");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.ParametersTypes)} = new string[] {{ {parameterTypes} }},");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.HasReturnValue)} = {hasReturnValue.GetBoolString()},");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.ReturnType)} = \"{me.ReturnType.GetTypeString()}\",");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.ReturnsTask)} = {(returnsTask && !returnsTaskWithResult).GetBoolString()},");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.ReturnsTaskWithResult)} = {returnsTaskWithResult.GetBoolString()},");
            sourceBuilder.AppendLine($"                {nameof(IProxyInvocation.TaskResultType)} = \"{taskResultType?.GetTypeString() ?? ""}\"");
            sourceBuilder.AppendLine("            };");
            sourceBuilder.AppendLine();

            if(!hasReturnValue && !returnsTask)
                sourceBuilder.AppendLine("            _interceptor.InterceptMethod(invocation);");
            else if(returnsTask && !returnsTaskWithResult)
                sourceBuilder.AppendLine($"                return _interceptor.InterceptMethodAsync(invocation);");
            else if(returnsTaskWithResult)
                sourceBuilder.AppendLine($"                return _interceptor.InterceptMethodAsync<{taskResultType?.ToDisplayString()}>(invocation);");
            else
                sourceBuilder.AppendLine($"                return ({returnType})_interceptor.InterceptMethod(invocation);");
  
            sourceBuilder.AppendLine("        }");
        }
    }
}
