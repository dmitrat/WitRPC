using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Options for WitRPC client hosted service.
    /// </summary>
    public class WitClientHostedServiceOptions
    {
        /// <summary>
        /// Gets or sets the name of the client to manage.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to auto-connect on startup.
        /// </summary>
        public bool AutoConnect { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Hosted service that manages WitRPC client lifecycle.
    /// </summary>
    public class WitClientHostedService : IHostedService
    {
        private readonly WitClientFactory _factory;
        private readonly WitClientHostedServiceOptions _options;
        private readonly ILogger<WitClientHostedService>? _logger;

        public WitClientHostedService(
            WitClientFactory factory,
            WitClientHostedServiceOptions options,
            ILogger<WitClientHostedService>? logger = null)
        {
            _factory = factory;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.AutoConnect)
            {
                _logger?.LogDebug("Auto-connect disabled for WitRPC client '{ClientName}'", _options.ClientName);
                return;
            }

            try
            {
                _logger?.LogInformation("Connecting WitRPC client '{ClientName}'...", _options.ClientName);
                
                var client = _factory.GetClient(_options.ClientName);
                var connected = await client.ConnectAsync(_options.ConnectionTimeout, cancellationToken);
                
                if (connected)
                    _logger?.LogInformation("WitRPC client '{ClientName}' connected successfully", _options.ClientName);
                else
                    _logger?.LogWarning("WitRPC client '{ClientName}' failed to connect", _options.ClientName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error connecting WitRPC client '{ClientName}'", _options.ClientName);
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Disconnecting WitRPC client '{ClientName}'...", _options.ClientName);
                
                var client = _factory.GetClient(_options.ClientName);
                await client.Disconnect();
                
                _logger?.LogInformation("WitRPC client '{ClientName}' disconnected", _options.ClientName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disconnecting WitRPC client '{ClientName}'", _options.ClientName);
            }
        }
    }
}
