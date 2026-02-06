using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring <see cref="WitClientBuilderContext"/> with services resolved from <see cref="IServiceProvider"/>.
    /// </summary>
    public static class WitClientBuilderContextExtensions
    {
        #region Logger

        /// <summary>
        /// Configures the logger by resolving <typeparamref name="TLogger"/> from the service provider.
        /// </summary>
        /// <typeparam name="TLogger">The logger type to resolve, e.g. <c>ILogger&lt;MyApp&gt;</c>.</typeparam>
        /// <param name="context">The client builder context.</param>
        /// <returns>The client builder context for chaining.</returns>
        public static WitClientBuilderContext WithLogger<TLogger>(this WitClientBuilderContext context)
            where TLogger : ILogger
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Options.Logger = context.ServiceProvider.GetRequiredService<TLogger>();
            return context;
        }

        /// <summary>
        /// Configures the logger by resolving <see cref="ILoggerFactory"/> from the service provider
        /// and creating a logger with the specified category name.
        /// </summary>
        /// <param name="context">The client builder context.</param>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>The client builder context for chaining.</returns>
        public static WitClientBuilderContext WithLogger(this WitClientBuilderContext context, string categoryName)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (categoryName == null)
                throw new ArgumentNullException(nameof(categoryName));

            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
            context.Options.Logger = loggerFactory.CreateLogger(categoryName);
            return context;
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Configures the access token provider by resolving <typeparamref name="TProvider"/> from the service provider.
        /// </summary>
        /// <typeparam name="TProvider">The access token provider type to resolve.</typeparam>
        /// <param name="context">The client builder context.</param>
        /// <returns>The client builder context for chaining.</returns>
        public static WitClientBuilderContext WithAccessTokenProvider<TProvider>(this WitClientBuilderContext context)
            where TProvider : IAccessTokenProvider
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Options.TokenProvider = context.ServiceProvider.GetRequiredService<TProvider>();
            return context;
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Configures the encryptor by resolving <typeparamref name="TEncryptor"/> from the service provider.
        /// </summary>
        /// <typeparam name="TEncryptor">The encryptor type to resolve.</typeparam>
        /// <param name="context">The client builder context.</param>
        /// <returns>The client builder context for chaining.</returns>
        public static WitClientBuilderContext WithEncryptor<TEncryptor>(this WitClientBuilderContext context)
            where TEncryptor : IEncryptorClient
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Options.Encryptor = context.ServiceProvider.GetRequiredService<TEncryptor>();
            return context;
        }

        #endregion
    }
}
