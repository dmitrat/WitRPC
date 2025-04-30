using System;
using System.Collections.Concurrent;
using MessagePack;
using MessagePack.Formatters;

namespace OutWit.Common.MessagePack
{
    internal class MessagePackResolver : IFormatterResolver
    {
        #region Classes

        private static class Cache<T>
        {
            static Cache()
            {
                Formatter = m_formatters.OfType<IMessagePackFormatter<T>>().FirstOrDefault();
                if (Formatter != null)
                    return;

                foreach (var resolver in m_resolvers)
                {
                    Formatter = resolver.GetFormatter<T>();
                    if (Formatter != null)
                        break;
                }
            }

            public static IMessagePackFormatter<T>? Formatter { get; }
        }

        #endregion
        
        #region Fields

        private static readonly ConcurrentBag<IMessagePackFormatter> m_formatters = new();
        
        private static readonly ConcurrentBag<IFormatterResolver> m_resolvers = new();

        #endregion

        #region Constructors

        private MessagePackResolver()
        {
            
        }

        #endregion

        #region Functions

        public void Register<TFormatter>()
            where TFormatter : IMessagePackFormatter, new()
        {
            m_formatters.Add(new TFormatter());
        }

        public void Register(IMessagePackFormatter formatter)
        {
            m_formatters.Add(formatter);
        }

        public void Register(IFormatterResolver resolver)
        {
            m_resolvers.Add(resolver);
        }

        #endregion

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }
        
        public static MessagePackResolver Instance { get; } = new();
      
    }
}
