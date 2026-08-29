using System;
using OutWit.Common.MessagePack;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Serializers.MessagePack
{
    /// <summary>
    /// Selects MessagePack for method parameters, return values and event
    /// arguments. Works on the client and the server builder options alike.
    /// </summary>
    public static class MessagePackSerializationExtensions
    {
        #region Functions

        /// <summary>
        /// Serializes user payloads with MessagePack-CSharp; models keep their
        /// existing <c>[MessagePackObject]</c>/<c>[Key]</c> annotations.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithMessagePack<TOptions>(this TOptions me)
            where TOptions : ISerializationOptions
        {
            me.ParametersSerializer = new MessageSerializerMessagePack();
            return me;
        }

        /// <summary>
        /// Serializes user payloads with MessagePack-CSharp after applying the
        /// given resolver/options configuration process-wide.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <param name="options">MessagePack configuration (resolvers, compression, ...).</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithMessagePack<TOptions>(this TOptions me, Action<MessagePackOptions> options)
            where TOptions : ISerializationOptions
        {
            MessagePackUtils.Register(options);

            return me.WithMessagePack();
        }

        #endregion
    }
}
