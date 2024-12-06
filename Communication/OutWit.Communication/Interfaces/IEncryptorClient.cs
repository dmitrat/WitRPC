using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptorClient : IEncryptor
    {
        public byte[] GetPublicKey();

        public byte[] GetPrivateKey();

        public bool ResetAes(byte[] symmetricKey, byte[] vector);
    }
}
