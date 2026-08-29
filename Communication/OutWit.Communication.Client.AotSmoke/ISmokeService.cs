using System.Threading.Tasks;
using OutWit.Common.Proxy.Attributes;

namespace OutWit.Communication.Client.AotSmoke
{
    /// <summary>
    /// A deliberately tiny contract: enough for the source generator to emit
    /// SmokeServiceProxy and for the linker to walk the full static call path
    /// (builder, transport, serializers, interceptor). The smoke server links
    /// this file as shared source, so the round-trip also exercises the
    /// assembly-independent contract ids.
    /// </summary>
    [ProxyTarget("SmokeServiceProxy")]
    public interface ISmokeService
    {
        string Echo(string message);

        Task<int> AddAsync(int a, int b);
    }
}
