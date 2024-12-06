using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptorServer : IEncryptor
    {
        public byte[] GetSymmetricKey();

        public byte[] GetVector();

        public void Reset();
    }
}
