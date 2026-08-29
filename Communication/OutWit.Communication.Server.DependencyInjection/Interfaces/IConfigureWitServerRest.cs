using System;

namespace OutWit.Communication.Server.DependencyInjection.Interfaces
{
    /// <summary>
    /// A named REST server configuration registered in the container; the
    /// factory applies it to a fresh context the first time the server is asked for.
    /// </summary>
    public interface IConfigureWitServerRest
    {
        string Name { get; }

        void Configure(WitServerRestBuilderContext context);
    }
}
