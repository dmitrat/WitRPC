using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Contracts
{
    public interface IBenchmarkService
    {
        public Task<long> OneWayBenchmark(byte[] bytes);

        public Task<byte[]> TwoWaysBenchmark(byte[] bytes);
    }
}
