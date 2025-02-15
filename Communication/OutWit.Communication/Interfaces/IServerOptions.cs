using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IServerOptions
    {
        public string Transport { get; }

        public Dictionary<string, string> Data { get; }
    }
}
