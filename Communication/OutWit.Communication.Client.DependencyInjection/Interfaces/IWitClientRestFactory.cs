using System;
using OutWit.Communication.Client.Rest;

namespace OutWit.Communication.Client.DependencyInjection.Interfaces
{
    /// <summary>
    /// Resolves named REST clients registered with <c>AddWitRpcRestClient</c>
    /// and builds service proxies over them.
    /// </summary>
    public interface IWitClientRestFactory
    {
        WitClientRest GetClient(string name);

        TService GetService<TService>(string name, bool strongAssemblyMatch = true) where TService : class;
    }
}
