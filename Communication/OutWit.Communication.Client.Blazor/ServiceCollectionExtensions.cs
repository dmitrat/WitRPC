using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Extension methods for registering WitRPC services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds WitRPC channel factory and related services for Blazor WebAssembly.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>Service collection for chaining</returns>
        /// <example>
        /// builder.Services.AddWitRpcChannel(options => 
        /// {
        ///     options.ApiPath = "api";
        ///     options.TimeoutSeconds = 15;
        /// });
        /// </example>
        public static IServiceCollection AddWitRpcChannel(
            this IServiceCollection services,
            Action<ChannelFactoryOptions>? configure = null)
        {
            var options = new ChannelFactoryOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);

            services.TryAddScoped<ChannelTokenProvider>(sp => new ChannelTokenProvider(
                sp.GetService<IAccessTokenProvider>(),
                sp.GetRequiredService<ILogger<ChannelTokenProvider>>()));

            services.TryAddScoped<IChannelFactory>(sp => new ChannelFactory(
                sp.GetRequiredService<NavigationManager>(),
                sp.GetService<AuthenticationStateProvider>(),
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<ChannelTokenProvider>(),
                sp.GetRequiredService<ChannelFactoryOptions>(),
                sp.GetRequiredService<ILogger<ChannelFactory>>()));

            return services;
        }
    }
}
