using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.HealthChecks
{
    /// <summary>
    /// Extension methods for adding WitRPC client health checks.
    /// </summary>
    public static class WitClientHealthCheckExtensions
    {
        /// <summary>
        /// Adds a health check for a WitRPC client.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="clientName">The name of the client configuration.</param>
        /// <param name="name">The name of the health check (defaults to "witrpc-client-{clientName}").</param>
        /// <param name="failureStatus">The failure status to report (defaults to Unhealthy).</param>
        /// <param name="tags">Tags for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddWitRpcClient(
            this IHealthChecksBuilder builder,
            string clientName,
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null)
        {
            return builder.Add(new HealthCheckRegistration(
                name ?? $"witrpc-client-{clientName}",
                sp =>
                {
                    var factory = sp.GetRequiredService<IWitClientFactory>();
                    return new WitClientHealthCheck(factory, clientName);
                },
                failureStatus,
                tags));
        }
    }
}
