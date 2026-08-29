using System;
using OutWit.Communication.Server.DependencyInjection.Interfaces;

namespace OutWit.Communication.Server.DependencyInjection
{
    internal sealed class ConfigureWitServerRest : IConfigureWitServerRest
    {
        #region Fields

        private readonly Action<WitServerRestBuilderContext> m_configure;

        #endregion

        #region Constructors

        public ConfigureWitServerRest(string name, Action<WitServerRestBuilderContext> configure)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            m_configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        #endregion

        #region IConfigureWitServerRest

        public void Configure(WitServerRestBuilderContext context)
        {
            m_configure(context);
        }

        public string Name { get; }

        #endregion
    }
}
