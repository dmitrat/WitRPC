using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Interfaces
{
    public interface IDiscoveryServer : IDisposable
    {
        event DiscoveryServerEventHandler DiscoveryMessageRequested;

        public Task<bool> Start(ILogger? logger = null);

        public Task<bool> SendDiscoveryMessage(byte[] data, ILogger? logger = null);

        public Task Stop();
    }

    public delegate void DiscoveryServerEventHandler(IDiscoveryServer sender); 
}
