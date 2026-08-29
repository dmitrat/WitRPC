using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client.Rest;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// The DI-flavoured REST client options: the same <c>With*</c> calls as the
    /// <see cref="WitClientRestBuilder"/>, resolving their arguments from the container.
    /// </summary>
    public static class WitClientRestBuilderContextExtensions
    {
        #region Logger

        public static WitClientRestBuilderContext WithLogger<TLogger>(this WitClientRestBuilderContext context)
            where TLogger : ILogger
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Logger = context.ServiceProvider.GetRequiredService<TLogger>();
            return context;
        }

        public static WitClientRestBuilderContext WithLogger(this WitClientRestBuilderContext context, string categoryName)
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

        public static WitClientRestBuilderContext WithAccessTokenProvider<TProvider>(this WitClientRestBuilderContext context)
            where TProvider : IAccessTokenProvider
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.TokenProvider = context.ServiceProvider.GetRequiredService<TProvider>();
            return context;
        }

        #endregion

        #region Http

        /// <summary>
        /// Sends every call through the named <see cref="HttpClient"/> from
        /// <see cref="IHttpClientFactory"/> (<c>services.AddHttpClient("name")</c>),
        /// so handlers, resilience and default headers are configured the usual way.
        /// </summary>
        public static WitClientRestBuilderContext WithHttpClient(this WitClientRestBuilderContext context, string name)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            context.HttpClient = context.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
            return context;
        }

        #endregion
    }
}
