using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Client.AotSmoke;
using OutWit.Communication.Server;
using OutWit.Communication.Server.WebSocket.Utils;

namespace OutWit.Communication.Client.AotSmoke.Server
{
    /// <summary>
    /// The live half of the NativeAOT smoke round-trip: a plain (JIT) WitRPC
    /// server hosting <see cref="ISmokeService"/> over WebSocket, configured to
    /// mirror the AOT client (MemoryPack, encryption, no authorization). The
    /// smoke script starts it, waits for the READY line, runs the AOT binary
    /// against it, and shuts it down.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: OutWit.Communication.Client.AotSmoke.Server <http-prefix>");
                return 2;
            }

            using var server = WitServerBuilder.Build(options =>
            {
                options.WithWebSocket(args[0], 1);
                options.WithMemoryPack();
                options.WithEncryption();
                options.WithoutAuthorization();
                options.WithService<ISmokeService>(new SmokeService());
            });

            server.StartWaitingForConnection();

            Console.WriteLine("SMOKE-SERVER READY");

            // Lives until the smoke script kills it.
            Thread.Sleep(Timeout.Infinite);
            return 0;
        }
    }

    public sealed class SmokeService : ISmokeService
    {
        public string Echo(string message)
        {
            return message;
        }

        public Task<int> AddAsync(int a, int b)
        {
            return Task.FromResult(a + b);
        }
    }
}
