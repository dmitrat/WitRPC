using System;
using System.Text;
using Microsoft.CodeAnalysis;
using OutWit.Common.Proxy.Interfaces;

namespace OutWit.Common.Proxy.Generator.Generators
{
    public static class EventGenerator
    {
        public static void Generate(this IEventSymbol me, StringBuilder sourceBuilder)
        {
            var eventType = me.Type.ToDisplayString();
            var eventName = me.Name;

            sourceBuilder.AppendLine($"        private event {eventType} _{eventName};");

            sourceBuilder.AppendLine($"        public event {eventType} {eventName}");
            sourceBuilder.AppendLine("        {");

            me.GenerateSubscribe(sourceBuilder);
            me.GenerateUnsubscribe(sourceBuilder);

            sourceBuilder.AppendLine("        }");
        }

        private static void GenerateSubscribe(this IEventSymbol me, StringBuilder sourceBuilder)
        {
            sourceBuilder.AppendLine("            add");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                var invocation = new OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("                {");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.MethodName)} = \"add_{me.Name}\",");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.Parameters)} = new object[] {{ value }},");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.ParametersTypes)} = new string[] {{ \"{me.Name}\" }},");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.ReturnType)} = \"void\"");
            sourceBuilder.AppendLine("                };");
            sourceBuilder.AppendLine("                _interceptor.Intercept(invocation);");
            sourceBuilder.AppendLine($"                _{me.Name} += value;");
            sourceBuilder.AppendLine("            }");
        }

        private static void GenerateUnsubscribe(this IEventSymbol me, StringBuilder sourceBuilder)
        {
            sourceBuilder.AppendLine("            remove");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                var invocation = new OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("                {");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.MethodName)} = \"remove_{me.Name}\",");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.Parameters)} = new object[] {{ value }},");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.ParametersTypes)} = new string[] {{ \"{me.Name}\" }},");
            sourceBuilder.AppendLine($"                    {nameof(IProxyInvocation.ReturnType)} = \"void\"");
            sourceBuilder.AppendLine("                };");
            sourceBuilder.AppendLine("                _interceptor.Intercept(invocation);");
            sourceBuilder.AppendLine($"                _{me.Name} -= value;");
            sourceBuilder.AppendLine("            }");
        }
    }
}
