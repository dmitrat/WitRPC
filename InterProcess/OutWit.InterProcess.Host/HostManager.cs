using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Exceptions;
using OutWit.Communication.Client;
using OutWit.InterProcess.Interfaces;

namespace OutWit.InterProcess.Host
{
    public class HostManager<TService> : IAgentManager<TService>
        where TService : class
    {
        #region Constructors

        public HostManager(WitComClientBuilderOptions options, string servicePath, TimeSpan processTimeout)
        {
            Options = options;
            ServicePath = servicePath;
            ProcessTimeout = processTimeout;
            Agents = new Dictionary<Guid, HostAgent<TService>>();
        }

        ~HostManager()
        {
            Dispose();
        }

        #endregion

        #region Functions

        public async Task<IAgent<TService>> CreateClient(TimeSpan timeout)
        {
            HostAgent<TService>? client = null;

            try
            {
                client = new HostAgent<TService>();

                if (!client.Start(Options, ServicePath, ProcessTimeout))
                    throw new ExceptionOf<HostManager<HostAgent<TService>>>($"Can not open client: {typeof(TService).Name}");

                if (!await client.Initialize(timeout) || !client.IsInitialized)
                    throw new ExceptionOf<HostManager<HostAgent<TService>>>($"Can initialize client: {typeof(TService).Name}");

                Agents.Add(client.Id, client);

                client.Disposed += DisposeClient;

                return client;

            }
            catch (Exception e)
            {
                client?.Dispose();
                throw;
            }
        }

        public IAgent<TService>? GetAgent(Guid id)
        {
            return Agents.GetValueOrDefault(id);
        }

        public bool ShutdownAgent(Guid id)
        {
            if(!Agents.TryGetValue(id, out HostAgent<TService>? agent))
                return false;

            agent.Shutdown();

            return true;
        }

        private void DisposeClient(Guid id)
        {
            if(!Agents.Remove(id, out var agent))
                return;

            agent.Dispose();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var id in Agents.Keys.ToList())
                DisposeClient(id);
        }

        #endregion

        #region Properties

        protected WitComClientBuilderOptions Options { get; }

        protected string ServicePath { get; }

        protected TimeSpan ProcessTimeout { get; }

        private Dictionary<Guid, HostAgent<TService>> Agents { get; }

        #endregion
    }
}
