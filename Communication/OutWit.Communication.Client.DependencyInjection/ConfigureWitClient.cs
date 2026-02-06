using System;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Implementation of <see cref="IConfigureWitClient"/> with access to <see cref="IServiceProvider"/>
    /// through <see cref="WitClientBuilderContext"/>.
    /// </summary>
    internal sealed class ConfigureWitClient : IConfigureWitClient
    {
        #region Fields

        private readonly Action<WitClientBuilderContext> m_configure;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureWitClient"/>.
        /// </summary>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">The configuration action.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="configure"/> is null.</exception>
        public ConfigureWitClient(string name, Action<WitClientBuilderContext> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitClient

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public void Configure(WitClientBuilderContext context)
        {
            m_configure(context);
        }

        #endregion
    }
}
