using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Server;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring <see cref="WitServerBuilderContext"/> with services resolved from <see cref="IServiceProvider"/>.
    /// </summary>
    public static class WitServerBuilderContextExtensions
    {
        #region Logger

        /// <summary>
        /// Configures the logger by resolving <typeparamref name="TLogger"/> from the service provider.
        /// </summary>
        /// <typeparam name="TLogger">The logger type to resolve, e.g. <c>ILogger&lt;MyServer&gt;</c>.</typeparam>
        /// <param name="context">The server builder context.</param>
        /// <returns>The server builder context for chaining.</returns>
        public static WitServerBuilderContext WithLogger<TLogger>(this WitServerBuilderContext context)
            where TLogger : ILogger
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Logger = context.ServiceProvider.GetRequiredService<TLogger>();
            return context;
        }

        /// <summary>
        /// Configures the logger by resolving <see cref="ILoggerFactory"/> from the service provider
        /// and creating a logger with the specified category name.
        /// </summary>
        /// <param name="context">The server builder context.</param>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>The server builder context for chaining.</returns>
        public static WitServerBuilderContext WithLogger(this WitServerBuilderContext context, string categoryName)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (categoryName == null)
                throw new ArgumentNullException(nameof(categoryName));

            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
            context.Logger = loggerFactory.CreateLogger(categoryName);
            return context;
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Configures the access token validator by resolving <typeparamref name="TValidator"/> from the service provider.
        /// </summary>
        /// <typeparam name="TValidator">The access token validator type to resolve.</typeparam>
        /// <param name="context">The server builder context.</param>
        /// <returns>The server builder context for chaining.</returns>
        public static WitServerBuilderContext WithAccessTokenValidator<TValidator>(this WitServerBuilderContext context)
            where TValidator : IAccessTokenValidator
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.TokenValidator = context.ServiceProvider.GetRequiredService<TValidator>();
            return context;
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Configures the encryptor factory by resolving <typeparamref name="TEncryptorFactory"/> from the service provider.
        /// </summary>
        /// <typeparam name="TEncryptorFactory">The encryptor factory type to resolve.</typeparam>
        /// <param name="context">The server builder context.</param>
        /// <returns>The server builder context for chaining.</returns>
        public static WitServerBuilderContext WithEncryptor<TEncryptorFactory>(this WitServerBuilderContext context)
            where TEncryptorFactory : IEncryptorServerFactory
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.EncryptorFactory = context.ServiceProvider.GetRequiredService<TEncryptorFactory>();
            return context;
        }

        #endregion

        #region Service

        /// <summary>
        /// Configures the request processor by resolving <typeparamref name="TService"/> from the service provider.
        /// </summary>
        /// <typeparam name="TService">The service interface type to resolve.</typeparam>
        /// <param name="context">The server builder context.</param>
        /// <param name="isStrongAssemblyMatch">Whether to use strong assembly matching.</param>
        /// <returns>The server builder context for chaining.</returns>
        public static WitServerBuilderContext WithService<TService>(this WitServerBuilderContext context, bool isStrongAssemblyMatch = true)
            where TService : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var service = context.ServiceProvider.GetRequiredService<TService>();
            context.WithService(service, isStrongAssemblyMatch);
            return context;
        }

        #endregion
    }
}
