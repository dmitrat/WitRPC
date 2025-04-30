using System;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Encryption;

namespace OutWit.Communication.Server
{
    public class WitComServerBuilderOptions
    {
        #region Constructors

        public WitComServerBuilderOptions()
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

        #endregion
    }
}
