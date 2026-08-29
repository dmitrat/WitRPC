using System;
using OutWit.Communication.Server.Rest;

namespace OutWit.Communication.Server.DependencyInjection.Interfaces
{
    /// <summary>
    /// Resolves named REST servers registered with <c>AddWitRpcRestServer</c>;
    /// each name is built once and shared.
    /// </summary>
    public interface IWitServerRestFactory
    {
        WitServerRest GetServer(string name);
    }
}
