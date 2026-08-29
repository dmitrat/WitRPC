using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Serializers;
using OutWit.Communication.Serializers.GoogleProtobuf;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;

namespace OutWit.Communication.Tests.Serializers
{
    /// <summary>
    /// The proto-first gRPC migration story: protoc-generated messages travel
    /// as protobuf wire bytes, everything else falls back to JSON, and a real
    /// client/server pair agrees on both without any marker on the wire.
    /// <see cref="Timestamp"/> stands in for a user's generated message -- it
    /// is one, shipped inside Google.Protobuf itself.
    /// </summary>
    [TestFixture]
    public class MessageSerializerGoogleProtobufTests
    {
        #region Constants

        private static readonly TimeSpan CONNECT_TIMEOUT = TimeSpan.FromSeconds(5);

        #endregion

        #region Serializer Tests

        [Test]
        public void ProtoMessageRoundTripsAsWireBytesTest()
        {
            var serializer = new MessageSerializerGoogleProtobuf();
            var stamp = Timestamp.FromDateTime(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc));

            byte[] bytes = serializer.Serialize(stamp);

            Assert.That(bytes, Is.EqualTo(Google.Protobuf.MessageExtensions.ToByteArray(stamp)),
                "a generated message must be written exactly as protobuf would");

            var generic = serializer.Deserialize<Timestamp>(bytes);
            var typed = (Timestamp?)serializer.Deserialize(bytes, typeof(Timestamp));

            Assert.That(generic, Is.EqualTo(stamp));
            Assert.That(typed, Is.EqualTo(stamp));
        }

        [Test]
        public void NonProtoPayloadUsesTheFallbackTest()
        {
            var fallback = new MessageSerializerJson();
            var serializer = new MessageSerializerGoogleProtobuf(fallback);

            byte[] number = serializer.Serialize(42, typeof(int));
            byte[] text = serializer.Serialize("hello", typeof(string));

            Assert.That(number, Is.EqualTo(fallback.Serialize(42, typeof(int))));
            Assert.That(serializer.Deserialize(number, typeof(int)), Is.EqualTo(42));
            Assert.That(serializer.Deserialize<string>(text), Is.EqualTo("hello"));
        }

        [Test]
        public void EmptyBytesDeserializeToNullTest()
        {
            var serializer = new MessageSerializerGoogleProtobuf();

            Assert.That(serializer.Deserialize<Timestamp>(Array.Empty<byte>()), Is.Null);
            Assert.That(serializer.Deserialize(Array.Empty<byte>(), typeof(Timestamp)), Is.Null);
        }

        [Test]
        public void GarbageBytesDeserializeToNullTest()
        {
            var serializer = new MessageSerializerGoogleProtobuf();

            Assert.That(serializer.Deserialize<Timestamp>(new byte[] { 0xFF, 0xFF, 0xFF }), Is.Null);
        }

        #endregion

        #region Round-Trip Tests

        [Test]
        public async Task ProtoMessagesAndPrimitivesCrossARealChannelTest()
        {
            string pipe = Shared.ChannelName(nameof(ProtoMessagesAndPrimitivesCrossARealChannelTest));

            using var server = WitServerBuilder.Build(options =>
            {
                options.WithNamedPipe(pipe, maxNumberOfClients: 1);
                options.WithGoogleProtobuf();
                options.WithService<IProtoEchoService>(new ProtoEchoService());
            });

            server.StartWaitingForConnection();

            using var client = WitClientBuilder.Build(options =>
            {
                options.WithNamedPipe(pipe);
                options.WithGoogleProtobuf();
            });

            Assert.That(await client.ConnectAsync(CONNECT_TIMEOUT, CancellationToken.None), Is.True);

            var service = client.GetService<IProtoEchoService>();

            var stamp = Timestamp.FromDateTime(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc));
            var shifted = service.AddSeconds(stamp, 90);

            Assert.That(shifted, Is.EqualTo(Timestamp.FromDateTime(stamp.ToDateTime().AddSeconds(90))));
            Assert.That(service.Add(19, 23), Is.EqualTo(42));
            Assert.That(service.Describe(stamp), Is.EqualTo(stamp.ToDateTime().ToString("O")));

            await client.Disconnect();
        }

        #endregion

        #region Contract

        public interface IProtoEchoService
        {
            Timestamp AddSeconds(Timestamp value, int seconds);

            int Add(int a, int b);

            string Describe(Timestamp value);
        }

        private sealed class ProtoEchoService : IProtoEchoService
        {
            public Timestamp AddSeconds(Timestamp value, int seconds)
            {
                return Timestamp.FromDateTime(value.ToDateTime().AddSeconds(seconds));
            }

            public int Add(int a, int b)
            {
                return a + b;
            }

            public string Describe(Timestamp value)
            {
                return value.ToDateTime().ToString("O");
            }
        }

        #endregion
    }
}
