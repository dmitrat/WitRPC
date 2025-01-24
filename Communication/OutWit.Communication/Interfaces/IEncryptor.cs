using System;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptor : IDisposable
    {
        public Task<byte[]> Encrypt(byte[] data);

        public Task<byte[]> Decrypt(byte[] data);
    }
}
