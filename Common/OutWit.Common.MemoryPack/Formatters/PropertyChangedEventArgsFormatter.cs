using System;
using MemoryPack;
using System.ComponentModel;

namespace OutWit.Common.MemoryPack.Formatters
{
    internal class PropertyChangedEventArgsFormatter: MemoryPackFormatter<PropertyChangedEventArgs?>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PropertyChangedEventArgs? value)
        {
            if (value is null)
                writer.WriteNullObjectHeader();
            else
            {
                writer.WriteObjectHeader(1);
                writer.WriteString(value.PropertyName);
            }
                
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PropertyChangedEventArgs? value)
        {
            if (!reader.TryReadObjectHeader(out var count))
            {
                value = null;
                return;
            }

            string? propName = count > 0
                ? reader.ReadString()
                : null;

            value = new PropertyChangedEventArgs(propName);
        }
    }
}
