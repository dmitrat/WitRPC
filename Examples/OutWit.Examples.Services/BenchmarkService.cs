using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Examples.Contracts;
using OutWit.Examples.Contracts.Utils;

namespace OutWit.Examples.Services
{
    public class BenchmarkService : IBenchmarkService
    {
        public async Task<long> OneWayBenchmark(byte[] bytes)
        {
            return HashUtils.ComputeFnv1aHash(bytes);
        }

        public async Task<byte[]> TwoWaysBenchmark(byte[] bytes)
        {
            return bytes;
        }
    }
}
