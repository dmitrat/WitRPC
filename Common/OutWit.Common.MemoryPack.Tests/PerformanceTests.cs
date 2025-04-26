using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Collections;
using NUnit.Framework;
using OutWit.Common.MemoryPack.Tests.Utils;

namespace OutWit.Common.MemoryPack.Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        #region Constants

        private const int PERFORMANCE_ARRAY_SIZE = 1_000_000;

        #endregion

        [Test]
        public void ConversionPerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var bytes = values.ToMemoryPackBytes();
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");

                start = DateTime.Now;

                var values1 = bytes.FromMemoryPackBytes<int[]>();

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }


    }
}
