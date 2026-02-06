using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Extends <see cref="WitServerBuilderOptions"/> with access to <see cref="IServiceProvider"/>
    /// during server configuration. All builder extension methods (e.g. <c>WithJson</c>, <c>WithNamedPipe</c>)
    /// work directly on this type. Additional extension methods on this type can resolve services
    /// without requiring the caller to pass <see cref="IServiceProvider"/> explicitly.
    /// </summary>
    public class WitServerBuilderContext : WitServerBuilderOptions
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="WitServerBuilderContext"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
        public WitServerBuilderContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        #endregion
    }
}
