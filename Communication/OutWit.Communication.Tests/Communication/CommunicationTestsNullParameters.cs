using System;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Communication
{
    [TestFixture]
    public class CommunicationTestsNullParameters
    {
        #region Single Null Parameter Tests

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task NullStringParameterTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(NullStringParameterTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // Test with null parameter
            Assert.That(service.RequestDataNullable(null), Is.EqualTo("nullable"));
            
            // Test with non-null parameter
            Assert.That(service.RequestDataNullable("test"), Is.EqualTo("test"));
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task NullStringParameterAsyncTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(NullStringParameterAsyncTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // Test with null parameter
            Assert.That(await service.RequestDataNullableAsync(null), Is.EqualTo("nullable"));
            
            // Test with non-null parameter
            Assert.That(await service.RequestDataNullableAsync("test"), Is.EqualTo("test"));
        }

        #endregion

        #region Null Return Value Tests

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task NullReturnValueTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(NullReturnValueTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // Test returning null
            Assert.That(service.RequestWithNullableResult(null), Is.Null);
            
            // Test returning non-null
            Assert.That(service.RequestWithNullableResult("test"), Is.EqualTo("test"));
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task NullReturnValueAsyncTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(NullReturnValueAsyncTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // Test returning null
            Assert.That(await service.RequestWithNullableResultAsync(null), Is.Null);
            
            // Test returning non-null
            Assert.That(await service.RequestWithNullableResultAsync("test"), Is.EqualTo("test"));
        }

        #endregion

        #region Multiple Nullable Parameters Tests

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task MultipleNullableParametersTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(MultipleNullableParametersTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // All nulls
            Assert.That(service.RequestWithMultipleNullableParams(null, null, null), Is.EqualTo("null|null|null"));
            
            // First non-null
            Assert.That(service.RequestWithMultipleNullableParams("test", null, null), Is.EqualTo("test|null|null"));
            
            // Second non-null
            Assert.That(service.RequestWithMultipleNullableParams(null, 42, null), Is.EqualTo("null|42|null"));
            
            // Third non-null
            Assert.That(service.RequestWithMultipleNullableParams(null, null, new ComplexNumber<int, int>(1, 2)), Is.EqualTo("null|null|(1,2)"));
            
            // All non-null
            Assert.That(service.RequestWithMultipleNullableParams("test", 42, new ComplexNumber<int, int>(1, 2)), Is.EqualTo("test|42|(1,2)"));
            
            // Mixed
            Assert.That(service.RequestWithMultipleNullableParams("hello", null, new ComplexNumber<int, int>(3, 4)), Is.EqualTo("hello|null|(3,4)"));
        }

        [TestCase(TransportType.MMF, SerializerType.Json)]
        [TestCase(TransportType.MMF, SerializerType.MessagePack)]
        [TestCase(TransportType.MMF, SerializerType.MemoryPack)]
        [TestCase(TransportType.MMF, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Pipes, SerializerType.Json)]
        [TestCase(TransportType.Pipes, SerializerType.MessagePack)]
        [TestCase(TransportType.Pipes, SerializerType.MemoryPack)]
        [TestCase(TransportType.Pipes, SerializerType.ProtoBuf)]

        [TestCase(TransportType.Tcp, SerializerType.Json)]
        [TestCase(TransportType.Tcp, SerializerType.MessagePack)]
        [TestCase(TransportType.Tcp, SerializerType.MemoryPack)]
        [TestCase(TransportType.Tcp, SerializerType.ProtoBuf)]

        [TestCase(TransportType.TcpSecure, SerializerType.Json)]
        [TestCase(TransportType.TcpSecure, SerializerType.MessagePack)]
        [TestCase(TransportType.TcpSecure, SerializerType.MemoryPack)]
        [TestCase(TransportType.TcpSecure, SerializerType.ProtoBuf)]

        [TestCase(TransportType.WebSocket, SerializerType.Json)]
        [TestCase(TransportType.WebSocket, SerializerType.MessagePack)]
        [TestCase(TransportType.WebSocket, SerializerType.MemoryPack)]
        [TestCase(TransportType.WebSocket, SerializerType.ProtoBuf)]
        public async Task MultipleNullableParametersAsyncTest(TransportType transportType, SerializerType serializerType)
        {
            var testName = $"{nameof(MultipleNullableParametersAsyncTest)}_{transportType}_{serializerType}";

            var server = Shared.GetServer(transportType, serializerType, 1, testName);
            server.StartWaitingForConnection();

            var client = Shared.GetClient(transportType, serializerType, testName);

            Assert.That(await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None), Is.True);

            var service = Shared.GetServiceDynamic(client);

            // All nulls
            Assert.That(await service.RequestWithMultipleNullableParamsAsync(null, null, null), Is.EqualTo("null|null|null"));
            
            // All non-null
            Assert.That(await service.RequestWithMultipleNullableParamsAsync("test", 42, new ComplexNumber<int, int>(1, 2)), Is.EqualTo("test|42|(1,2)"));
            
            // Mixed
            Assert.That(await service.RequestWithMultipleNullableParamsAsync(null, 42, null), Is.EqualTo("null|42|null"));
        }

        #endregion
    }
}
