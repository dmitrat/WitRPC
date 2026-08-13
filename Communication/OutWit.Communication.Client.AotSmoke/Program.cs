using System;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Attributes;
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;

namespace OutWit.Communication.Client.AotSmoke
{
    // A deliberately tiny contract: enough for the source generator to emit
    // SmokeServiceProxy and for the linker to walk the full static call path
    // (builder, transport, serializers, interceptor). No server is contacted;
    // publishing this project IS the test.
    [ProxyTarget("SmokeServiceProxy")]
    public interface ISmokeService
    {
        string Echo(string message);

        Task<int> AddAsync(int a, int b);
    }

    public static class Program
    {
        public static int Main()
        {
            var client = WitClientBuilder.Build(options =>
            {
                options.WithWebSocket("wss://localhost:5001/smoke");
                options.WithMemoryPack();
                options.WithEncryption();
                options.WithoutAuthorization();
                options.WithTimeout(TimeSpan.FromSeconds(1));
            });

            // The static path under test: generated proxy, no runtime emission.
            ISmokeService service = client.GetService<ISmokeService>(
                interceptor => new SmokeServiceProxy(interceptor));

            // Touch the proxy so trimming cannot drop it; no call is made.
            Console.WriteLine(service.GetType().FullName);
            return 0;
        }
    }
}
