using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Benchmark.Client.Model
{
    public class BenchmarkResult
    {
        public int SeriesId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Transport { get; set; }

        public string Serializer { get; set; }

        public bool UseEncryption { get; set; }

        public bool UseAuthorization { get; set; }

        public TimeSpan Duration { get; set; }

        public double DurationMs { get; set; }

        public bool Success { get; set; }
    }
}
