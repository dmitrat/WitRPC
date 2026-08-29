using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Exceptions;
using OutWit.Communication.Client;
using OutWit.InterProcess.Interfaces;

namespace OutWit.InterProcess.Host
{
    /// <summary>
    /// Keeps a registry of running agents for one service type. Every agent gets
    /// its own options from the factory -- transports and addresses are
    /// per-process, never shared -- and the registry itself is synchronized: an
    /// agent that dies removes itself, a disposed manager takes every agent's
    /// process down with it.
    /// </summary>
    public class HostManager<TService> : IAgentManager<TService>
        where TService : class
    {
        #region Fields

        private readonly object m_lock = new();

        private readonly Dictionary<Guid, HostAgent<TService>> m_agents = new();

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <param name="optionsFactory">
        /// Builds the client options for one agent. Called once per agent, so
        /// each gets its own transport and address; returning a shared instance
        /// would put two processes on one endpoint.
        /// </param>
        /// <param name="servicePath">Path to the agent executable.</param>
        /// <param name="processTimeout">The agents' idle-shutdown timeout (zero for none).</param>
        public HostManager(Func<WitClientBuilderOptions> optionsFactory, string servicePath, TimeSpan processTimeout)
        {
            OptionsFactory = optionsFactory;
            ServicePath = servicePath;
            ProcessTimeout = processTimeout;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Spawns a new agent process, connects to it and registers it. Throws
        /// (leaving nothing behind) when the process cannot start or does not
        /// answer within <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">How long to wait for the agent's endpoint to come up.</param>
        /// <returns>The running, initialized agent.</returns>
        public async Task<IAgent<TService>> CreateClient(TimeSpan timeout)
        {
            HostAgent<TService>? client = null;

            try
            {
                client = new HostAgent<TService>();

                if (!client.Start(OptionsFactory(), ServicePath, ProcessTimeout))
                    throw new ExceptionOf<HostManager<TService>>($"Can not open client: {typeof(TService).Name}");

                if (!await client.Initialize(timeout) || !client.IsInitialized)
                    throw new ExceptionOf<HostManager<TService>>($"Can not initialize client: {typeof(TService).Name}");

                // Subscribed before registration so an agent that dies in the
                // gap still removes itself; the re-check below closes the rest
                // of the window.
                client.Disposed += OnAgentDisposed;

                lock (m_lock)
                    m_agents.Add(client.Id, client);

                if (!client.IsInitialized)
                {
                    OnAgentDisposed(client.Id);
                    throw new ExceptionOf<HostManager<TService>>($"Client died during registration: {typeof(TService).Name}");
                }

                return client;
            }
            catch (Exception)
            {
                client?.Dispose();
                throw;
            }
        }

        /// <summary>Returns the registered agent, or null when it is gone.</summary>
        /// <param name="id">The agent's id.</param>
        public IAgent<TService>? GetAgent(Guid id)
        {
            lock (m_lock)
                return m_agents.GetValueOrDefault(id);
        }

        /// <summary>
        /// Hard-kills the agent's process tree. The agent removes itself from
        /// the registry through its Disposed event.
        /// </summary>
        /// <param name="id">The agent's id.</param>
        /// <returns>True when the agent was known.</returns>
        public bool ShutdownAgent(Guid id)
        {
            HostAgent<TService>? agent;

            lock (m_lock)
                agent = m_agents.GetValueOrDefault(id);

            if (agent == null)
                return false;

            agent.Shutdown();
            return true;
        }

        #endregion

        #region Event Handlers

        private void OnAgentDisposed(Guid id)
        {
            HostAgent<TService>? agent;

            lock (m_lock)
            {
                if (!m_agents.Remove(id, out agent))
                    return;
            }

            agent!.Disposed -= OnAgentDisposed;
            agent.Dispose();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            List<Guid> ids;

            lock (m_lock)
            {
                if (m_disposed)
                    return;

                m_disposed = true;
                ids = m_agents.Keys.ToList();
            }

            foreach (var id in ids)
                OnAgentDisposed(id);
        }

        #endregion

        #region Properties

        protected Func<WitClientBuilderOptions> OptionsFactory { get; }

        protected string ServicePath { get; }

        protected TimeSpan ProcessTimeout { get; }

        #endregion
    }
}
