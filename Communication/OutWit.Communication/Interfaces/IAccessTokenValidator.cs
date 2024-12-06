using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IAccessTokenValidator
    {
        public bool IsRequestTokenValid(string token);

        public bool IsAuthorizationTokenValid(string token);
    }
}
