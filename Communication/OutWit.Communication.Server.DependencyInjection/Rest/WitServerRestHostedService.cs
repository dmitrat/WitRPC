using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Starts every auto-start REST server with the host and stops it with the host.
    /// </summary>
    public sealed class WitServerRestHostedService : IHostedService
    {
        #region Fields

        private readonly WitServerRestFactory m_factory;

        private readonly IEnumerable<WitServerRestHostedServiceOptions> m_options;

        private readonly ILogger<WitServerRestHostedService>? m_logger;

        #endregion

        #region Constructors

        public WitServerRestHostedService(WitServerRestFactory factory, IEnumerable<WitServerRestHostedServiceOptions> options,
            ILogger<WitServerRestHostedService>? logger = null)
        {
            m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger;
        }

        #endregion

        #region IHostedService

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!options.AutoStart)
                    continue;

                try
                {
                    m_logger?.LogInformation("Starting WitRPC REST server '{ServerName}'...", options.ServerName);
                    m_factory.GetServer(options.ServerName).StartWaitingForConnection();
                    m_logger?.LogInformation("WitRPC REST server '{ServerName}' started", options.ServerName);
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "Error starting WitRPC REST server '{ServerName}'", options.ServerName);
                    throw;
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!m_factory.HasServer(options.ServerName))
                    continue;

                try
                {
                    m_factory.GetServer(options.ServerName).StopWaitingForConnection();
                    m_logger?.LogInformation("WitRPC REST server '{ServerName}' stopped", options.ServerName);
                }
                catch (Exception e)
                {
                    m_logger?.LogError(e, "Error stopping WitRPC REST server '{ServerName}'", options.ServerName);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
