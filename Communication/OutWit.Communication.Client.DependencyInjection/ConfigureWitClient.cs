using System;
using OutWit.Communication.Client;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// Default implementation of <see cref="IConfigureWitClient"/>.
    /// </summary>
    internal sealed class ConfigureWitClient : IConfigureWitClient
    {
        #region Fields

        private readonly Action<WitClientBuilderOptions> m_configure;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigureWitClient"/>.
        /// </summary>
        /// <param name="name">The name of the client configuration.</param>
        /// <param name="configure">The configuration action.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="configure"/> is null.</exception>
        public ConfigureWitClient(string name, Action<WitClientBuilderOptions> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitClient

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public void Configure(WitClientBuilderOptions options)
        {
            m_configure(options);
        }

        #endregion
    }
}
