using System;
using OutWit.Communication.Server;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Implementation of <see cref="IConfigureWitServer"/> with access to <see cref="IServiceProvider"/>.
    /// </summary>
    internal sealed class ConfigureWitServer : IConfigureWitServer
    {
        #region Fields

        private readonly Action<WitServerBuilderOptions, IServiceProvider> m_configure;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureWitServer"/>.
        /// </summary>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">The configuration action.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="configure"/> is null.</exception>
        public ConfigureWitServer(string name, Action<WitServerBuilderOptions, IServiceProvider> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitServer

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public void Configure(WitServerBuilderOptions options, IServiceProvider serviceProvider)
        {
            m_configure(options, serviceProvider);
        }

        #endregion
    }
}
