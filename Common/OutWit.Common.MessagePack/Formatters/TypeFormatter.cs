using System;
using MessagePack;
using MessagePack.Formatters;

namespace OutWit.Common.MessagePack.Formatters
{
    internal class TypeFormatter : IMessagePackFormatter<Type?>
    {
        public void Serialize(ref MessagePackWriter writer, Type? value, MessagePackSerializerOptions options)
        {
            writer.Write(value?.AssemblyQualifiedName);
        }

        public Type? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil()) 
                return null;
            
            var name = reader.ReadString();
            
            return string.IsNullOrEmpty(name) 
                ? null
                : Type.GetType(name, throwOnError: true)!;
        }
    }
}
