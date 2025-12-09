using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Server;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Options for WitRPC server hosted service.
    /// </summary>
    public class WitServerHostedServiceOptions
    {
        /// <summary>
        /// Gets or sets the name of the server configuration.
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to auto-start on startup.
        /// </summary>
        public bool AutoStart { get; set; } = true;
    }

    /// <summary>
    /// Hosted service that manages WitRPC server lifecycle.
    /// </summary>
    public class WitServerHostedService : IHostedService
    {
        private readonly WitServerFactory _factory;
        private readonly WitServerHostedServiceOptions _options;
        private readonly ILogger<WitServerHostedService>? _logger;

        public WitServerHostedService(
            WitServerFactory factory,
            WitServerHostedServiceOptions options,
            ILogger<WitServerHostedService>? logger = null)
        {
            _factory = factory;
            _options = options;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.AutoStart)
            {
                _logger?.LogDebug("Auto-start disabled for WitRPC server '{ServerName}'", _options.ServerName);
                return Task.CompletedTask;
            }

            try
            {
                _logger?.LogInformation("Starting WitRPC server '{ServerName}'...", _options.ServerName);
                
                var server = _factory.GetServer(_options.ServerName);
                server.StartWaitingForConnection();
                
                _logger?.LogInformation("WitRPC server '{ServerName}' started successfully", _options.ServerName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting WitRPC server '{ServerName}'", _options.ServerName);
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("Stopping WitRPC server '{ServerName}'...", _options.ServerName);
                
                var server = _factory.GetServer(_options.ServerName);
                server.StopWaitingForConnection();
                
                _logger?.LogInformation("WitRPC server '{ServerName}' stopped", _options.ServerName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping WitRPC server '{ServerName}'", _options.ServerName);
            }

            return Task.CompletedTask;
        }
    }
}
