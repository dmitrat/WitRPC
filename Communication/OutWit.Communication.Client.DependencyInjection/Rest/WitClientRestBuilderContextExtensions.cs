using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// The DI-flavoured REST client options: resolve the token provider from the container.
    /// </summary>
    public static class WitClientRestBuilderContextExtensions
    {
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
    }
}
