using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Interfaces
{
    public interface IDiscoveryClient : IDisposable
    {
        public event DiscoveryClientEventHandler MessageReceived;

        public Task<bool> Start(ILogger? logger = null);

        public Task Stop();
    }

    public delegate void DiscoveryClientEventHandler(IDiscoveryClient sender, DiscoveryMessage message);
}
