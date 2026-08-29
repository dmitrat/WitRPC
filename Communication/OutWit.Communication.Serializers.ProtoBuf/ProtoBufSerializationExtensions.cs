using System;
using OutWit.Common.ProtoBuf;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Serializers.ProtoBuf
{
    /// <summary>
    /// Selects protobuf-net for method parameters, return values and event
    /// arguments. Works on the client and the server builder options alike.
    /// </summary>
    public static class ProtoBufSerializationExtensions
    {
        #region Functions

        /// <summary>
        /// Serializes user payloads with protobuf-net; models keep their
        /// existing <c>[ProtoContract]</c>/<c>[ProtoMember]</c> (or
        /// <c>[DataContract]</c>/<c>[DataMember]</c>) annotations.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithProtoBuf<TOptions>(this TOptions me)
            where TOptions : ISerializationOptions
        {
            me.ParametersSerializer = new MessageSerializerProtoBuf();
            return me;
        }

        /// <summary>
        /// Serializes user payloads with protobuf-net after applying the given
        /// configuration process-wide.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <param name="options">protobuf-net configuration.</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithProtoBuf<TOptions>(this TOptions me, Action<ProtoBufOptions> options)
            where TOptions : ISerializationOptions
        {
            ProtoBufUtils.Register(options);

            return me.WithProtoBuf();
        }

        #endregion
    }
}
