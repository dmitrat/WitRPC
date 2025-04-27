using System;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Utils;
using OutWit.Communication.Utils;
using OutWit.Communication.Messages;

namespace OutWit.Communication.Tests.Messages
{
    [TestFixture]
    public class DiscoveryMessageTests
    {
        private const string GUID = "4FDCD14D-D727-4ACC-B522-16B6665D8626";

        [Test]
        public void ConstructorTest()
        {
            var message = new DiscoveryMessage();
            Assert.That(message.ServiceId, Is.EqualTo(null));
            Assert.That(message.Timestamp, Is.EqualTo(null));
            Assert.That(message.Type, Is.EqualTo(null));
            Assert.That(message.ServiceName, Is.EqualTo(null));
            Assert.That(message.ServiceDescription, Is.EqualTo(null));
            Assert.That(message.Transport, Is.EqualTo(null));
            Assert.That(message.Data, Is.EqualTo(null));

            message = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10", "11" }
                }
            };
            Assert.That(message.ServiceId, Is.EqualTo(Guid.Parse(GUID)));
            Assert.That(message.Timestamp, Is.EqualTo(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero)));
            Assert.That(message.Type, Is.EqualTo(DiscoveryMessageType.Heartbeat));
            Assert.That(message.ServiceName, Is.EqualTo("7"));
            Assert.That(message.ServiceDescription, Is.EqualTo("8"));
            Assert.That(message.Transport, Is.EqualTo("9"));
            Assert.That(message.Data, Is.EqualTo(new Dictionary<string, string>
            {
                { "10", "11" }
            }));
        }

        [Test]
        public void IsTest()
        {
            var message = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };

            Assert.That(message.Is(message.Clone()), Is.True);
            Assert.That(message.Is(message.With(x => x.ServiceId = Guid.NewGuid())), Is.False);
            Assert.That(message.Is(message.With(x => x.Timestamp = DateTimeOffset.UtcNow)), Is.False);
            Assert.That(message.Is(message.With(x => x.Type = DiscoveryMessageType.Goodbye)), Is.False);
            Assert.That(message.Is(message.With(x => x.ServiceName = "8")), Is.False);
            Assert.That(message.Is(message.With(x => x.ServiceDescription = "9")), Is.False);
            Assert.That(message.Is(message.With(x => x.Transport = "10")), Is.False);
            Assert.That(message.Is(message.With(x => x.Data = new Dictionary<string, string>
            {
                {"10", "12"}
            })), Is.False);
        }

        [Test]
        public void CloneTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };
            var message2 = message1.Clone() as DiscoveryMessage;

            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message2.ServiceId, Is.EqualTo(Guid.Parse(GUID)));
            Assert.That(message2.Timestamp, Is.EqualTo(new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero)));
            Assert.That(message2.Type, Is.EqualTo(DiscoveryMessageType.Heartbeat));
            Assert.That(message2.ServiceName, Is.EqualTo("7"));
            Assert.That(message2.ServiceDescription, Is.EqualTo("8"));
            Assert.That(message2.Transport, Is.EqualTo("9"));
            Assert.That(message2.Data, Is.EqualTo(new Dictionary<string, string>
            {
                { "10", "11" }
            }));
        }

        [Test]
        public void JsonCloneTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };
            var message2 = message1.JsonClone() as DiscoveryMessage;

            Assert.That(message2, Is.Not.Null);
            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };

            var bytes = message1.ToPackBytes();
            Assert.That(bytes, Is.Not.Null);

            var message2 = bytes.FromPackBytes<DiscoveryMessage>();
            Assert.That(message2, Is.Not.Null);
            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);

        }

        [Test]
        public void JsonSerializationTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };

            var json = message1.ToJsonBytes();
            Assert.That(json, Is.Not.Null);

            var message2 = json.FromJsonBytes<DiscoveryMessage>();
            Assert.That(message2, Is.Not.Null);
            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };

            var json = message1.ToMemoryPackBytes();
            Assert.That(json, Is.Not.Null);

            var message2 = json.FromMemoryPackBytes<DiscoveryMessage>();
            Assert.That(message2, Is.Not.Null);
            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var message1 = new DiscoveryMessage
            {
                ServiceId = Guid.Parse(GUID),
                Timestamp = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
                Type = DiscoveryMessageType.Heartbeat,
                ServiceName = "7",
                ServiceDescription = "8",
                Transport = "9",
                Data = new Dictionary<string, string>
                {
                    { "10" , "11" }
                }
            };

            var json = message1.ToProtoBytes();
            Assert.That(json, Is.Not.Null);

            var message2 = json.FromProtoBytes<DiscoveryMessage>();
            Assert.That(message2, Is.Not.Null);
            Assert.That(message1, Is.Not.SameAs(message2));
            Assert.That(message1.Is(message2), Is.True);
        }
    }
}
