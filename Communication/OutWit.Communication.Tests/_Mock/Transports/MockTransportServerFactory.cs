using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Tests.Mock.Transports
{
    /// <summary>
    /// A transport factory the test drives: <see cref="Connect"/> hands a
    /// <see cref="MockTransportServer"/> to the server as a new client.
    /// </summary>
    public sealed class MockTransportServerFactory : ITransportServerFactory
    {
        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        #endregion

        #region Functions

        /// <summary>
        /// Presents a new connection to the server.
        /// </summary>
        public MockTransportServer Connect()
        {
            var transport = new MockTransportServer();
            NewClientConnected(transport);
            return transport;
        }

        #endregion

        #region ITransportServerFactory

        public void StartWaitingForConnection(ILogger? logger)
        {
            IsStarted = true;
        }

        public void StopWaitingForConnection()
        {
            IsStarted = false;
        }

        public void Dispose()
        {
        }

        #endregion

        #region Properties

        public IServerOptions Options { get; } = new MockServerOptions();

        public bool IsStarted { get; private set; }

        #endregion

        #region Nested Types

        private sealed class MockServerOptions : IServerOptions
        {
            public string Transport => "Mock";

            public Dictionary<string, string> Data { get; } = new();
        }

        #endregion
    }
}
