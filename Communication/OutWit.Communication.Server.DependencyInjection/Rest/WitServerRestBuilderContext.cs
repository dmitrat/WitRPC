using System;
using OutWit.Communication.Server.Rest;

namespace OutWit.Communication.Server.DependencyInjection
{
    /// <summary>
    /// The REST builder options plus the container, so a registration can pull
    /// its service, validator and logger out of DI.
    /// </summary>
    public class WitServerRestBuilderContext : WitServerRestBuilderOptions
    {
        #region Constructors

        public WitServerRestBuilderContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region Properties

        public IServiceProvider ServiceProvider { get; }

        #endregion
    }
}
