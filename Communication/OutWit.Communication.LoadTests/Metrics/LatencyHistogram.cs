namespace OutWit.Communication.LoadTests.Metrics
{
    /// <summary>
    /// Collects latencies in microseconds and answers percentiles. Thread-safe for adds.
    /// </summary>
    public sealed class LatencyHistogram
    {
        #region Fields

        private readonly object m_gate = new();

        private readonly List<double> m_samples = new();

        #endregion

        #region Functions

        public void Add(double microseconds)
        {
            lock (m_gate)
                m_samples.Add(microseconds);
        }

        public LatencySummary Summarize()
        {
            double[] sorted;
            lock (m_gate)
                sorted = m_samples.OrderBy(sample => sample).ToArray();

            if (sorted.Length == 0)
                return new LatencySummary(0, 0, 0, 0, 0, 0);

            return new LatencySummary(
                sorted.Length,
                Percentile(sorted, 0.50),
                Percentile(sorted, 0.95),
                Percentile(sorted, 0.99),
                sorted[^1],
                sorted.Average());
        }

        /// <summary>
        /// A copy of the samples, for merging into another histogram.
        /// </summary>
        public double[] Drain()
        {
            lock (m_gate)
                return m_samples.ToArray();
        }

        private static double Percentile(double[] sorted, double p)
        {
            var index = (int)Math.Ceiling(p * sorted.Length) - 1;
            return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
        }

        #endregion

        #region Properties

        public int Count
        {
            get
            {
                lock (m_gate)
                    return m_samples.Count;
            }
        }

        #endregion
    }

    /// <summary>
    /// Percentiles in milliseconds.
    /// </summary>
    public sealed record LatencySummary(int Count, double P50Us, double P95Us, double P99Us, double MaxUs, double MeanUs)
    {
        public double P50Ms => P50Us / 1000.0;

        public double P95Ms => P95Us / 1000.0;

        public double P99Ms => P99Us / 1000.0;

        public double MaxMs => MaxUs / 1000.0;

        public double MeanMs => MeanUs / 1000.0;

        public override string ToString()
        {
            return $"n={Count} p50={P50Ms:F2} ms p95={P95Ms:F2} ms p99={P99Ms:F2} ms max={MaxMs:F1} ms";
        }
    }
}
