using System;
using OutWit.Common.Collections;

namespace OutWit.Common.ProtoBuf.Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        #region Constants

        private const int PERFORMANCE_ARRAY_SIZE = 1_000_000;

        #endregion

        [Test]
        public void BytesSerializationGenericPerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var jsonBytes = values.ToProtoBytes();
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(jsonBytes, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonBytes.FromProtoBytes<int[]>();

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }

        [Test]
        public void BytesSerializationTypedPerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var jsonBytes = values.ToProtoBytes(typeof(int[]));
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(jsonBytes, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonBytes.FromProtoBytes(typeof(int[])) as int[];

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }

        [Test]
        public void ProtoBufClonePerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var values1 = values.ProtoClone();
                var end = DateTime.Now;

                Console.WriteLine($"Json Clone duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values1, Is.Not.Null);
                Assert.That(values.Is(values1), Is.True);
            }
        }

    }
}
