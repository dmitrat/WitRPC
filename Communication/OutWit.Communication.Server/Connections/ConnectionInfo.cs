using System;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Connections
{
    public class ConnectionInfo
    {
        #region Constructors

        public ConnectionInfo(ITransportServer transport, IEncryptorServer encryptor)
        {
            Transport = transport;
            Encryptor = encryptor;

            IsInitialized = false;
            IsAuthorized = false;
        }

        #endregion

        #region Properties

        public ITransportServer Transport { get; }

        public IEncryptorServer Encryptor { get; }

        public bool IsInitialized { get; set; }

        public bool IsAuthorized { get; set; }


        public Guid Id => Transport.Id;

        #endregion
    }
}
