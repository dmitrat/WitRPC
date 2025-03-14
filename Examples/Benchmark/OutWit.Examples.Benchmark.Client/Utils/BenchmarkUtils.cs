using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Benchmark.Client.Utils
{
    public static class BenchmarkUtils
    {
        private static Random m_random = new Random();

        private static int m_count = 0;

        public static int NextId()
        {
            return m_count++;
        }

        public static byte[] GenerateData(long dataSize)
        {
            var data = new byte[dataSize];

            m_random.NextBytes(data);

            return data;
        }
    }
}
