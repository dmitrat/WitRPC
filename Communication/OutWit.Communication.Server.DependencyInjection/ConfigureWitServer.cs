using System;
using OutWit.Communication.Server;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// Implementation of <see cref="IConfigureWitServer"/> with access to <see cref="IServiceProvider"/>
    /// through <see cref="WitServerBuilderContext"/>.
    /// </summary>
    internal sealed class ConfigureWitServer : IConfigureWitServer
    {
        #region Fields

        private readonly Action<WitServerBuilderContext> m_configure;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureWitServer"/>.
        /// </summary>
        /// <param name="name">The name of the server configuration.</param>
        /// <param name="configure">The configuration action.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="configure"/> is null.</exception>
        public ConfigureWitServer(string name, Action<WitServerBuilderContext> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitServer

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public void Configure(WitServerBuilderContext context)
        {
            m_configure(context);
        }

        #endregion
    }
}
