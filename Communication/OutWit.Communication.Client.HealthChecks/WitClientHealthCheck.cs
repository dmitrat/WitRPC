using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OutWit.Communication.Client.DependencyInjection.Interfaces;
using OutWit.Communication.Client.Reconnection;

namespace OutWit.Communication.Client.HealthChecks
{
    /// <summary>
    /// Health check for WitRPC client connectivity.
    /// </summary>
    public class WitClientHealthCheck : IHealthCheck
    {
        private readonly IWitClientFactory _clientFactory;
        private readonly string _clientName;

        public WitClientHealthCheck(IWitClientFactory clientFactory, string clientName)
        {
            _clientFactory = clientFactory;
            _clientName = clientName;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _clientFactory.GetClient(_clientName);

                var data = new Dictionary<string, object>
                {
                    ["clientName"] = _clientName,
                    ["isInitialized"] = client.IsInitialized,
                    ["isAuthorized"] = client.IsAuthorized,
                    ["connectionState"] = client.ConnectionState.ToString()
                };

                if (client.ConnectionState == ReconnectionState.Connected && 
                    client.IsInitialized && 
                    client.IsAuthorized)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        $"WitRPC client '{_clientName}' is connected and authorized.",
                        data));
                }

                if (client.ConnectionState == ReconnectionState.Reconnecting)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"WitRPC client '{_clientName}' is reconnecting.",
                        data: data));
                }

                if (client.ConnectionState == ReconnectionState.Failed)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"WitRPC client '{_clientName}' reconnection failed.",
                        data: data));
                }

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"WitRPC client '{_clientName}' is not connected. State: {client.ConnectionState}",
                    data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"WitRPC client '{_clientName}' health check failed.",
                    ex));
            }
        }
    }
}
