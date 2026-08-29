using System;
using OutWit.Communication.Client.Rest;

namespace OutWit.Communication.Client.DependencyInjection
{
    /// <summary>
    /// The REST client options plus the container, so a registration can pull
    /// its token provider out of DI.
    /// </summary>
    public class WitClientRestBuilderContext : WitClientRestBuilderOptions
    {
        #region Constructors

        public WitClientRestBuilderContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region Properties

        public IServiceProvider ServiceProvider { get; }

        #endregion
    }
}
