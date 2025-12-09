using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Resilience;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client
{
    public class WitClientBuilderOptions
    {
        #region Constructors

        public WitClientBuilderOptions()
        {
            ParametersSerializer = new MessageSerializerJson();
            MessageSerializer = new MessageSerializerMemoryPack();
            Encryptor = new EncryptorClientPlain();
            TokenProvider = new AccessTokenProviderPlain();
            ReconnectionOptions = new ReconnectionOptions();
            RetryOptions = new RetryOptions();

            Logger = null;
            Timeout = null;
        }

        #endregion

        #region Properties

        public ITransportClient? Transport { get; set; }

        public IMessageSerializer ParametersSerializer { get; set; }
        
        public IMessageSerializer MessageSerializer { get; set; }

        public IEncryptorClient Encryptor { get; set; }

        public IAccessTokenProvider TokenProvider { get; set; }

        public ReconnectionOptions ReconnectionOptions { get; set; }

        public RetryOptions RetryOptions { get; set; }

        public ILogger? Logger { get; set; }

        public TimeSpan? Timeout { get; set; }

        #endregion
    }
}
