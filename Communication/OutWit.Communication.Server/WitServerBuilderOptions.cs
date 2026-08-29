using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Server
{
    public class WitServerBuilderOptions : ISerializationOptions
    {
        #region Constructors

        public WitServerBuilderOptions()
        {
            ParametersSerializer = new MessageSerializerJson();
            MessageSerializer = new MessageSerializerMemoryPack();
            EncryptorFactory = new EncryptorServerFactory<EncryptorServerPlain>();
            TokenValidator = new AccessTokenValidatorPlain();

            DiscoveryServer = null;

            Logger = null;
            Timeout = null;
        }

        #endregion

        #region Properties


        public ITransportServerFactory? TransportFactory { get; set; }

        public IRequestProcessor? RequestProcessor { get; set; }

        public IEncryptorServerFactory EncryptorFactory { get; set; }

        public IMessageSerializer ParametersSerializer { get; set; }
        
        public IMessageSerializer MessageSerializer { get; set; }

        public IAccessTokenValidator TokenValidator { get; set; }

        public IDiscoveryServer? DiscoveryServer { get; set; }

        public ILogger? Logger { get; set; }

        public TimeSpan? Timeout { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// The most service methods the server will run at once, across all
        /// connections. Each connection's own requests are always processed in
        /// order; this caps how many connections may be mid-call together.
        /// Unbounded by default. Set to 1 to serialise every call, as the server
        /// did before 3.0 — but note that service methods are now invoked
        /// concurrently across connections, so they must be thread-safe.
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = int.MaxValue;

        /// <summary>
        /// How long a connected client has to finish the handshake (initialize
        /// and authorize) before the server closes it. Stops a client that
        /// connects and then holds the socket open without ever authorizing from
        /// tying up a connection slot. Default 30 seconds; set to <c>null</c> or
        /// <see cref="TimeSpan.Zero"/> to disable.
        /// </summary>
        public TimeSpan? HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(30);

        #endregion
    }
}
