using System;
using OutWit.Communication.Client.DependencyInjection.Interfaces;

namespace OutWit.Communication.Client.DependencyInjection
{
    internal sealed class ConfigureWitClientRest : IConfigureWitClientRest
    {
        #region Fields

        private readonly Action<WitClientRestBuilderContext> m_configure;

        #endregion

        #region Constructors

        public ConfigureWitClientRest(string name, Action<WitClientRestBuilderContext> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitClientRest

        public void Configure(WitClientRestBuilderContext context)
        {
            m_configure(context);
        }

        public string Name { get; }

        #endregion
    }
}
