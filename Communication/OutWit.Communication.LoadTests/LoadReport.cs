using System.Text.Json;
using OutWit.Communication.LoadTests.Metrics;

namespace OutWit.Communication.LoadTests
{
    /// <summary>
    /// The numbers of one run, printed as a table and optionally written as JSON.
    /// </summary>
    public sealed class LoadReport
    {
        #region Properties

        public string Options { get; set; } = string.Empty;

        public int Nodes { get; set; }

        public double ConnectSeconds { get; set; }

        public int Dispatched { get; set; }

        public int Acked { get; set; }

        public double AchievedRatePerSecond { get; set; }

        public LatencySummary AllNodes { get; set; } = new(0, 0, 0, 0, 0, 0);

        public LatencySummary? NeighboursOfSlow { get; set; }

        public LatencySummary? SlowNode { get; set; }

        public Dictionary<string, int> Outcomes { get; set; } = new();

        public long SlowNodePendingMax { get; set; }

        public long AnyNodePendingMax { get; set; }

        public bool SlowNodeClosed { get; set; }

        public double SlowNodeClosedAfterSeconds { get; set; }

        public double ServerCpuSeconds { get; set; }

        public long WorkingSetMb { get; set; }

        public long BroadcastFramesPerNode { get; set; }

        #endregion

        #region Functions

        public void Print(TextWriter output)
        {
            output.WriteLine();
            output.WriteLine($"| run | {Options} |");
            output.WriteLine("|---|---|");
            output.WriteLine($"| nodes connected | {Nodes} in {ConnectSeconds:F2} s |");
            output.WriteLine($"| dispatched / acked | {Dispatched} / {Acked} ({AchievedRatePerSecond:F0} tasks/s achieved) |");
            output.WriteLine($"| dispatch -> ack, all nodes | {AllNodes} |");

            if (NeighboursOfSlow != null)
                output.WriteLine($"| dispatch -> ack, neighbours of the slow node | {NeighboursOfSlow} |");

            if (SlowNode != null)
                output.WriteLine($"| dispatch -> ack, the slow node | {SlowNode} |");

            if (Outcomes.Count > 0)
                output.WriteLine($"| delivery outcomes | {string.Join(", ", Outcomes.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}"))} |");

            if (SlowNodePendingMax > 0 || SlowNodeClosed)
                output.WriteLine($"| slow node queue max / closed | {SlowNodePendingMax} / {(SlowNodeClosed ? $"yes after {SlowNodeClosedAfterSeconds:F1} s" : "no")} |");

            output.WriteLine($"| queued callbacks, max over the other nodes | {AnyNodePendingMax} |");

            if (BroadcastFramesPerNode > 0)
                output.WriteLine($"| callback frames received per node | {BroadcastFramesPerNode} |");

            output.WriteLine($"| process CPU / working set | {ServerCpuSeconds:F1} s / {WorkingSetMb} MB |");
            output.WriteLine();
        }

        public void WriteJson(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }

        #endregion
    }
}
