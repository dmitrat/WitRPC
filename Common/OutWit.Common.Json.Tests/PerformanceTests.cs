using System;
using OutWit.Common.Collections;

namespace OutWit.Common.Json.Tests
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
                var jsonBytes = values.ToJsonBytes();
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");
                
                Assert.That(jsonBytes, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonBytes.FromJsonBytes<int[]>();

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
                var jsonBytes = values.ToJsonBytes(typeof(int[]));
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(jsonBytes, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonBytes.FromJsonBytes(typeof(int[])) as int[];

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }

        [Test]
        public void StringSerializationGenericPerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var jsonString = values.ToJsonString();
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");
                
                Assert.That(jsonString, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonString.FromJsonString<int[]>();

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }

        [Test]
        public void StringSerializationTypedPerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var jsonString = values.ToJsonString(typeof(int[]));
                var end = DateTime.Now;

                Console.WriteLine($"Conversion to bytes duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(jsonString, Is.Not.Null);

                start = DateTime.Now;

                var values1 = jsonString.FromJsonString(typeof(int[])) as int[];

                end = DateTime.Now;

                Console.WriteLine($"Conversion to values duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values.Is(values1), Is.True);
            }
        }

        [Test]
        public void JsonClonePerformanceTest()
        {
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;
                var values1 = values.JsonClone();
                var end = DateTime.Now;

                Console.WriteLine($"Json Clone duration: {(end - start).TotalMilliseconds:0.0000} ms");

                Assert.That(values1, Is.Not.Null);
                Assert.That(values.Is(values1), Is.True);
            }
        }

    }
}
