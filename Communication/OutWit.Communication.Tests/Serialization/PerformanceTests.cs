using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Tests.Serialization
{
    [TestFixture]
    public class PerformanceTests
    {
        #region Constants

        private const int PERFORMANCE_ARRAY_SIZE = 1_000_000;

        #endregion


        [TestCase(SerializerType.Json)]
        [TestCase(SerializerType.MessagePack)]
        [TestCase(SerializerType.MemoryPack)]
        [TestCase(SerializerType.ProtoBuf)]
        public async Task SerializationPerformance(SerializerType serializerType)
        {
            var serializer = Shared.GetSerializer(serializerType);
            
            int[] values = new int[PERFORMANCE_ARRAY_SIZE];
            var random = new System.Random();
            for (int i = 0; i < PERFORMANCE_ARRAY_SIZE; i++)
                values[i] = random.Next();

            for (int i = 0; i < 10; i++)
            {
                var start = DateTime.Now;

                var bytes = serializer.Serialize(values);

                var end = DateTime.Now;

                Console.WriteLine($"Parameter serialization duration: {(end - start).TotalMilliseconds:0.0000} ms");

                var request = new WitRequest
                {
                    Token = "token",
                    MethodName = "Serialization Test",
                    Parameters = new byte[][] { bytes },
                    ParameterTypes = new[] { typeof(int[]) },
                    ParameterTypesByName = new[] { (ParameterType)typeof(int[]) },
                    GenericArguments = Array.Empty<Type>(),
                    GenericArgumentsByName = Array.Empty<ParameterType>()
                };

                start = DateTime.Now;
                
                bytes = serializer.Serialize(request);
                
                end = DateTime.Now;

                Console.WriteLine($"Request serialization duration: {(end - start).TotalMilliseconds:0.0000} ms");

                var message = new WitMessage
                {
                    Id = Guid.NewGuid(),
                    Type = WitMessageType.Unknown,
                    Data = bytes
                };

                start = DateTime.Now;

                bytes = serializer.Serialize(message);

                end = DateTime.Now;

                Console.WriteLine($"Message serialization duration: {(end - start).TotalMilliseconds:0.0000} ms");
            }




        }
    }
}
