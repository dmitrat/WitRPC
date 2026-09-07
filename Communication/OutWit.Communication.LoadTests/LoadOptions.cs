using OutWit.Communication.Server.Callbacks;

namespace OutWit.Communication.LoadTests
{
    /// <summary>
    /// What one run measures. Parsed from <c>--name value</c> arguments; see <see cref="Usage"/>.
    /// </summary>
    public sealed class LoadOptions
    {
        #region Constants

        public const string Usage = """
            OutWit.Communication.LoadTests -- the 3.2 shared-server model under load

              --mode shared|per-client|broadcast   shared: one server, targeted callbacks (3.2)
                                                   per-client: one server per node (the 1.7.x baseline)
                                                   broadcast: one server, untargeted raise to everyone (what sharing
                                                   a server meant before 3.2; every node receives every task)
              --nodes N          simulated nodes (default 100)
              --rate R           tasks per second, total (default 500)
              --duration S       seconds of steady dispatch (default 20)
              --payload BYTES    task payload size (default 256)
              --transport websocket|tcp (default websocket)
              --slow K           make node K's socket slow (server-side write delay), -1 = none (default -1)
              --slow-delay MS    the delay per write of the slow node (default 50)
              --stall            instead of a delay, the slow node's socket never drains
              --max-pending N    CallbackDeliveryOptions.MaxPendingCallbacks (default 0 = unbounded)
              --policy log|close|drop   overflow policy (default log)
              --timeout MS       server callback send timeout (default 5000)
              --json PATH        also write the report as JSON
            """;

        #endregion

        #region Functions

        public static LoadOptions Parse(string[] args)
        {
            var options = new LoadOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var name = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{name} needs a value");

                switch (name)
                {
                    case "--mode": options.Mode = Next(); break;
                    case "--nodes": options.Nodes = int.Parse(Next()); break;
                    case "--rate": options.Rate = int.Parse(Next()); break;
                    case "--duration": options.DurationSeconds = int.Parse(Next()); break;
                    case "--payload": options.PayloadBytes = int.Parse(Next()); break;
                    case "--transport": options.Transport = Next(); break;
                    case "--slow": options.SlowNode = int.Parse(Next()); break;
                    case "--slow-delay": options.SlowDelayMs = int.Parse(Next()); break;
                    case "--stall": options.Stall = true; break;
                    case "--max-pending": options.MaxPending = int.Parse(Next()); break;
                    case "--policy": options.Policy = Next(); break;
                    case "--timeout": options.TimeoutMs = int.Parse(Next()); break;
                    case "--json": options.JsonPath = Next(); break;
                    case "--help": case "-h": options.Help = true; break;
                    default: throw new ArgumentException($"Unknown argument {name}\n{Usage}");
                }
            }

            return options;
        }

        public CallbackDeliveryOptions ToDelivery()
        {
            return new CallbackDeliveryOptions
            {
                MaxPendingCallbacks = MaxPending,
                OverflowPolicy = Policy switch
                {
                    "close" => CallbackOverflowPolicy.CloseConnection,
                    "drop" => CallbackOverflowPolicy.DropNewest,
                    _ => CallbackOverflowPolicy.Log
                },
                Mode = Mode == "shared" ? CallbackDeliveryMode.TargetedOnly : CallbackDeliveryMode.BroadcastAllowed
            };
        }

        public override string ToString()
        {
            var slow = SlowNode < 0 ? "none" : Stall ? $"node {SlowNode} stalled" : $"node {SlowNode} +{SlowDelayMs} ms/write";
            return $"mode={Mode} nodes={Nodes} rate={Rate}/s duration={DurationSeconds}s payload={PayloadBytes}B transport={Transport} slow={slow} maxPending={MaxPending} policy={Policy} timeout={TimeoutMs}ms";
        }

        #endregion

        #region Properties

        public string Mode { get; set; } = "shared";

        public int Nodes { get; set; } = 100;

        public int Rate { get; set; } = 500;

        public int DurationSeconds { get; set; } = 20;

        public int PayloadBytes { get; set; } = 256;

        public string Transport { get; set; } = "websocket";

        public int SlowNode { get; set; } = -1;

        public int SlowDelayMs { get; set; } = 50;

        public bool Stall { get; set; }

        public int MaxPending { get; set; }

        public string Policy { get; set; } = "log";

        public int TimeoutMs { get; set; } = 5000;

        public string? JsonPath { get; set; }

        public bool Help { get; set; }

        public bool WebSocket => Transport != "tcp";

        #endregion
    }
}
