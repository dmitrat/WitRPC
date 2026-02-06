using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Hosted service that manages WitRPC client lifecycle.
    /// Supports multiple client configurations when registered via <see cref="IEnumerable{WitClientHostedServiceOptions}"/>.
    /// </summary>
    public sealed class WitClientHostedService : IHostedService
    {
        #region Fields

        private readonly WitClientFactory m_factory;
        private readonly IEnumerable<WitClientHostedServiceOptions> m_options;
        private readonly ILogger<WitClientHostedService>? m_logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitClientHostedService"/>.
        /// </summary>
        /// <param name="factory">The client factory.</param>
        /// <param name="options">The hosted service options for each client to manage.</param>
        /// <param name="logger">An optional logger.</param>
        public WitClientHostedService(
            WitClientFactory factory,
            IEnumerable<WitClientHostedServiceOptions> options,
            ILogger<WitClientHostedService>? logger = null)
        {
            m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_options = options ?? throw new ArgumentNullException(nameof(options));
            m_logger = logger;
        }

        #endregion

        #region IHostedService

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!options.AutoConnect)
                {
                    m_logger?.LogDebug("Auto-connect disabled for WitRPC client '{ClientName}'", options.ClientName);
                    continue;
                }

                try
                {
                    m_logger?.LogInformation("Connecting WitRPC client '{ClientName}'...", options.ClientName);

                    var client = m_factory.GetClient(options.ClientName);
                    var connected = await client.ConnectAsync(options.ConnectionTimeout, cancellationToken);

                    if (connected)
                        m_logger?.LogInformation("WitRPC client '{ClientName}' connected successfully", options.ClientName);
                    else
                        m_logger?.LogWarning("WitRPC client '{ClientName}' failed to connect", options.ClientName);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError(ex, "Error connecting WitRPC client '{ClientName}'", options.ClientName);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var options in m_options)
            {
                if (!m_factory.HasClient(options.ClientName))
                    continue;

                try
                {
                    m_logger?.LogInformation("Disconnecting WitRPC client '{ClientName}'...", options.ClientName);

                    var client = m_factory.GetClient(options.ClientName);
                    await client.Disconnect();

                    m_logger?.LogInformation("WitRPC client '{ClientName}' disconnected", options.ClientName);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError(ex, "Error disconnecting WitRPC client '{ClientName}'", options.ClientName);
                }
            }
        }

        #endregion
    }
}
