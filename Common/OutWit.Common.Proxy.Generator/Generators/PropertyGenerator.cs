using System;
using System.Text;
using Microsoft.CodeAnalysis;
using OutWit.Common.Proxy.Generator.Utils;

namespace OutWit.Common.Proxy.Generator.Generators
{
    public static class PropertyGenerator
    {
        public static void Generate(this IPropertySymbol me, StringBuilder sourceBuilder)
        {
            var propertyType = me.Type.ToDisplayString();
            var propertyName = me.Name;

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        public {propertyType} {propertyName}");
            sourceBuilder.AppendLine("        {");

            me.GenerateGet(sourceBuilder);
            me.GenerateSet(sourceBuilder);

            sourceBuilder.AppendLine("        }");
        }

        private static void GenerateGet(this IPropertySymbol me, StringBuilder sourceBuilder)
        {
            if(me.GetMethod == null)
                return;

            sourceBuilder.AppendLine("            get");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                var invocation = new  OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("                {");
            sourceBuilder.AppendLine($"                    MethodName = \"get_{me.Name}\",");
            sourceBuilder.AppendLine("                     Parameters = new object[0],");
            sourceBuilder.AppendLine("                     ParameterTypes = new string[0],");
            sourceBuilder.AppendLine($"                    ReturnType = \"{me.Type.GetTypeString()}\"");
            sourceBuilder.AppendLine("                };");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("                _interceptor.Intercept(invocation);");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"                if (invocation.ReturnValue != null)");
            sourceBuilder.AppendLine($"                    return ({me.Type.ToDisplayString()})invocation.ReturnValue;");
            sourceBuilder.AppendLine($"                return default({me.Type.ToDisplayString()});");
            sourceBuilder.AppendLine("            }");
        }

        private static void GenerateSet(this IPropertySymbol me, StringBuilder sourceBuilder)
        {
            if (me.SetMethod == null)
                return;

            sourceBuilder.AppendLine("            set");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                var invocation = new OutWit.Common.Proxy.{nameof(ProxyInvocation)}");
            sourceBuilder.AppendLine("                {");
            sourceBuilder.AppendLine($"                    MethodName = \"set_{me.Name}\",");
            sourceBuilder.AppendLine("                     Parameters = new object[] { value },");
            sourceBuilder.AppendLine($"                    ParameterTypes = new string[] {{ \"{me.Type.GetTypeString()}\" }},");
            sourceBuilder.AppendLine($"                    ReturnType = \"void\"");

            sourceBuilder.AppendLine("                };");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("                _interceptor.Intercept(invocation);");
            sourceBuilder.AppendLine("            }");
        }
    }
}
