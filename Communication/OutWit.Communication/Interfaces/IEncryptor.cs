using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptor : IDisposable
    {
        public byte[] Encrypt(byte[] data);

        public byte[] Decrypt(byte[] data);
    }
}
