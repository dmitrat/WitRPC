using System;
using OutWit.Communication.Converters;
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
            Serializer = new MessageSerializerJson();
            Converter = new ValueConverterJson();
            EncryptorFactory = new EncryptorServerFactory<EncryptorServerPlain>();
            TokenValidator = new AccessTokenValidatorPlain();
        }

        #endregion

        #region Properties


        public ITransportServerFactory? TransportFactory { get; set; }

        public IRequestProcessor? RequestProcessor { get; set; }

        public IEncryptorServerFactory EncryptorFactory { get; set; }

        public IMessageSerializer Serializer { get; set; }

        public IValueConverter Converter { get; set; }

        public IAccessTokenValidator TokenValidator { get; set; }

        #endregion
    }
}
