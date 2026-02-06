using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Provides access to <see cref="WitClientBuilderOptions"/> and <see cref="IServiceProvider"/>
    /// during client configuration. Extension methods on this type can resolve services
    /// without requiring the caller to pass <see cref="IServiceProvider"/> explicitly.
    /// </summary>
    public sealed class WitClientBuilderContext
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitClientBuilderContext"/>.
        /// </summary>
        /// <param name="options">The client builder options.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="serviceProvider"/> is null.</exception>
        public WitClientBuilderContext(WitClientBuilderOptions options, IServiceProvider serviceProvider)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the client builder options.
        /// </summary>
        public WitClientBuilderOptions Options { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        #endregion
    }
}
