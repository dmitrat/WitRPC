using System;
using MessagePack;
using MessagePack.Formatters;

namespace OutWit.Common.MessagePack
{
    public class MessagePackOptions
    {
        internal MessagePackOptions()
        {
            
        }
        
        public ICollection<IMessagePackFormatter> Formatters { get; } = new List<IMessagePackFormatter>();

        public ICollection<IFormatterResolver> Resolvers { get; } = new List<IFormatterResolver>();
    }
}
