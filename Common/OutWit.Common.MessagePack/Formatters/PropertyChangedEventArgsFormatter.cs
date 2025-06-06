using System;
using System.ComponentModel;
using MessagePack.Formatters;
using MessagePack;

namespace OutWit.Common.MessagePack.Formatters
{
    internal class PropertyChangedEventArgsFormatter : IMessagePackFormatter<PropertyChangedEventArgs>
    {
        public void Serialize(ref MessagePackWriter writer, PropertyChangedEventArgs value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteArrayHeader(1);
            writer.Write(value.PropertyName);
        }

        public PropertyChangedEventArgs Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil()) 
                return null;
            
            var count = reader.ReadArrayHeader();
            if (count == 0) 
                return new PropertyChangedEventArgs(null);
            
            var propName = reader.ReadString();
            
            return new PropertyChangedEventArgs(propName);
        }
    }
}
