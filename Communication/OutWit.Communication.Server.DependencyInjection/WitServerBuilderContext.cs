using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Provides access to <see cref="WitServerBuilderOptions"/> and <see cref="IServiceProvider"/>
    /// during server configuration. Extension methods on this type can resolve services
    /// without requiring the caller to pass <see cref="IServiceProvider"/> explicitly.
    /// </summary>
    public sealed class WitServerBuilderContext
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitServerBuilderContext"/>.
        /// </summary>
        /// <param name="options">The server builder options.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="serviceProvider"/> is null.</exception>
        public WitServerBuilderContext(WitServerBuilderOptions options, IServiceProvider serviceProvider)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the server builder options.
        /// </summary>
        public WitServerBuilderOptions Options { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        #endregion
    }
}
