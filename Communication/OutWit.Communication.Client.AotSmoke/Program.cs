using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;

namespace OutWit.Communication.Client.AotSmoke
{
    /// <summary>
    /// The NativeAOT smoke. Publishing this project is the first half of the
    /// test (the full static call path survives trimming and ILC). Running the
    /// produced binary with a server URL is the second half: a real encrypted
    /// round-trip through the generated proxy against a live WitRPC server.
    /// With no arguments the binary only touches the proxy and exits -- the
    /// publish-gate mode.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : null;

            var client = WitClientBuilder.Build(options =>
            {
                options.WithWebSocket(url ?? "wss://localhost:5001/smoke");
                options.WithMemoryPack();
                options.WithEncryption();
                options.WithoutAuthorization();
                options.WithTimeout(TimeSpan.FromSeconds(10));
                options.WithLogger(new StderrLogger());
            });

            // The static path under test: generated proxy, no runtime emission.
            ISmokeService service = client.GetService<ISmokeService>(
                interceptor => new SmokeServiceProxy(interceptor));

            if (url == null)
            {
                // Publish-gate mode: touch the proxy so trimming cannot drop it.
                Console.WriteLine(service.GetType().FullName);
                return 0;
            }

            return RunRoundTripAsync(client, service).GetAwaiter().GetResult();
        }

        private static async Task<int> RunRoundTripAsync(WitClient client, ISmokeService service)
        {
            if (!await client.ConnectAsync(TimeSpan.FromSeconds(10), CancellationToken.None))
            {
                Console.Error.WriteLine("SMOKE FAILED: could not connect");
                return 1;
            }

            var echoed = service.Echo("round-trip");
            if (echoed != "round-trip")
            {
                Console.Error.WriteLine($"SMOKE FAILED: Echo returned '{echoed}'");
                return 1;
            }

            var sum = await service.AddAsync(19, 23);
            if (sum != 42)
            {
                Console.Error.WriteLine($"SMOKE FAILED: AddAsync returned {sum}");
                return 1;
            }

            await client.Disconnect();

            Console.WriteLine("SMOKE OK: encrypted round-trip through the AOT binary succeeded");
            return 0;
        }
    }

    /// <summary>
    /// A minimal logger so a failure inside the AOT binary names its exception
    /// instead of degrading to a silent false.
    /// </summary>
    internal sealed class StderrLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            Console.Error.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            if (exception != null)
                Console.Error.WriteLine(exception);
        }
    }
}
