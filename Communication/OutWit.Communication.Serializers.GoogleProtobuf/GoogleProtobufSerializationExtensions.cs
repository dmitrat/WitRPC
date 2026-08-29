using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Serializers.GoogleProtobuf
{
    /// <summary>
    /// Selects Google.Protobuf for protoc-generated parameters, results and
    /// event arguments. Works on the client and the server builder options alike.
    /// </summary>
    public static class GoogleProtobufSerializationExtensions
    {
        #region Functions

        /// <summary>
        /// Serializes <c>IMessage</c> payloads as protobuf wire bytes and every
        /// other payload as JSON.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithGoogleProtobuf<TOptions>(this TOptions me)
            where TOptions : ISerializationOptions
        {
            me.ParametersSerializer = new MessageSerializerGoogleProtobuf();
            return me;
        }

        /// <summary>
        /// Serializes <c>IMessage</c> payloads as protobuf wire bytes and every
        /// other payload with the given fallback serializer.
        /// </summary>
        /// <typeparam name="TOptions">The client or server builder options.</typeparam>
        /// <param name="me">The options being configured.</param>
        /// <param name="fallback">The serializer for non-protobuf payloads.</param>
        /// <returns>The same options, for chaining.</returns>
        public static TOptions WithGoogleProtobuf<TOptions>(this TOptions me, IMessageSerializer fallback)
            where TOptions : ISerializationOptions
        {
            me.ParametersSerializer = new MessageSerializerGoogleProtobuf(fallback);
            return me;
        }

        #endregion
    }
}
