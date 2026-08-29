using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Server.Rest;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// The DI-flavoured REST options: the same <c>With*</c> calls as the
    /// <see cref="WitServerRestBuilder"/>, resolving their arguments from the container.
    /// </summary>
    public static class WitServerRestBuilderContextExtensions
    {
        #region Logger

        public static WitServerRestBuilderContext WithLogger<TLogger>(this WitServerRestBuilderContext context)
            where TLogger : ILogger
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Logger = context.ServiceProvider.GetRequiredService<TLogger>();
            return context;
        }

        public static WitServerRestBuilderContext WithLogger(this WitServerRestBuilderContext context, string categoryName)
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

        public static WitServerRestBuilderContext WithAccessTokenValidator<TValidator>(this WitServerRestBuilderContext context)
            where TValidator : IAccessTokenValidator
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.TokenValidator = context.ServiceProvider.GetRequiredService<TValidator>();
            return context;
        }

        #endregion

        #region Service

        /// <summary>
        /// Exposes the <typeparamref name="TService"/> registered in the container.
        /// </summary>
        public static WitServerRestBuilderContext WithService<TService>(this WitServerRestBuilderContext context, bool isStrongAssemblyMatch = true)
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
