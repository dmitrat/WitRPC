using System;

namespace OutWit.Communication.Client.DependencyInjection.Interfaces
{
    /// <summary>
    /// A named REST client configuration registered in the container; the
    /// factory applies it to a fresh context the first time the client is asked for.
    /// </summary>
    public interface IConfigureWitClientRest
    {
        string Name { get; }

        void Configure(WitClientRestBuilderContext context);
    }
}
