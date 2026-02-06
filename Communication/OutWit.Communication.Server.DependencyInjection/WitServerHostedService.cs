using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Hosted service that manages WitRPC server lifecycle.
    /// Supports multiple server configurations when registered via <see cref="IEnumerable{WitServerHostedServiceOptions}"/>.
    /// </summary>
    public sealed class WitServerHostedService : IHostedService
    {
        #region Fields

        private readonly WitServerFactory m_factory;
        private readonly IEnumerable<WitServerHostedServiceOptions> m_options;
        private readonly ILogger<WitServerHostedService>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitServerHostedService"/>.
        /// </summary>
        /// <param name="factory">The server factory.</param>
        /// <param name="options">The hosted service options for each server to manage.</param>
        /// <param name="logger">An optional logger.</param>
        public WitServerHostedService(
            WitServerFactory factory,
            IEnumerable<WitServerHostedServiceOptions> options,
            ILogger<WitServerHostedService>? logger = null)
        {
            m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger;
        }

        #endregion

        #region IHostedService

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!options.AutoStart)
                {
                    m_logger?.LogDebug("Auto-start disabled for WitRPC server '{ServerName}'", options.ServerName);
                    continue;
                }

                try
                {
                    m_logger?.LogInformation("Starting WitRPC server '{ServerName}'...", options.ServerName);

                    var server = m_factory.GetServer(options.ServerName);
                    server.StartWaitingForConnection();

                    m_logger?.LogInformation("WitRPC server '{ServerName}' started successfully", options.ServerName);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError(ex, "Error starting WitRPC server '{ServerName}'", options.ServerName);
                    throw;
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!m_factory.HasServer(options.ServerName))
                    continue;

                try
                {
                    m_logger?.LogInformation("Stopping WitRPC server '{ServerName}'...", options.ServerName);

                    var server = m_factory.GetServer(options.ServerName);
                    server.StopWaitingForConnection();

                    m_logger?.LogInformation("WitRPC server '{ServerName}' stopped", options.ServerName);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError(ex, "Error stopping WitRPC server '{ServerName}'", options.ServerName);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
