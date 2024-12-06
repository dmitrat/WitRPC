using System;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Server.Encryption
{
    public class EncryptorServerFactory<TEncryptor> : IEncryptorServerFactory
        where TEncryptor : IEncryptorServer, new()
    {
        public IEncryptorServer CreateEncryptor()
        {
            return new TEncryptor();
        }
    }
}
