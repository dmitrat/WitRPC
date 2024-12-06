using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IEncryptorServerFactory
    {
        public IEncryptorServer CreateEncryptor();
    }
}
