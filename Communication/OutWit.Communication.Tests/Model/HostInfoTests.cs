using System;
using NUnit.Framework.Legacy;
using OutWit.Communication.Model;
using OutWit.Common.Json;
using OutWit.Common.MemoryPack;
using OutWit.Common.MessagePack;
using OutWit.Common.ProtoBuf;
using OutWit.Common.Utils;

namespace OutWit.Communication.Tests.Model
{
    [TestFixture]
    public class HostInfoTests
    {
        [Test]
        public void ConstructorTest()
        {
            var info = new HostInfo();

            Assert.That(info.Host, Is.EqualTo(""));
            Assert.That(info.Port, Is.Null);
            Assert.That(info.UseSsl, Is.False);
            Assert.That(info.UseWebSocket, Is.False);
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.Port, Is.EqualTo(2));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Path, Is.EqualTo("name"));
        }

        [Test]
        public void OperatorsTest()
        {
            var info = (HostInfo)"https://host:2/path/";
            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.Port, Is.EqualTo(2));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Path, Is.EqualTo("path"));
        }

        [Test]
        public void BuildConnectionTest()
        {
            var info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = false,
                Path = "path"
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("https://host:2/path/"));
            Assert.That(info.Connection, Is.EqualTo("https://host:2/path/"));

            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = false,
                UseWebSocket = false,
                Path = "path"
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("http://host:2/path/"));
            Assert.That(info.Connection, Is.EqualTo("http://host:2/path/"));

            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = false,
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("https://host:2/"));
            Assert.That(info.Connection, Is.EqualTo("https://host:2/"));

            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = false,
                UseWebSocket = false
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("http://host:2/"));
            Assert.That(info.Connection, Is.EqualTo("http://host:2/"));

            info = new HostInfo
            {
                Host = "host",
                UseSsl = false,
                UseWebSocket = false,
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("http://host/"));
            Assert.That(info.Connection, Is.EqualTo("http://host/"));


            info = new HostInfo
            {
                Host = "host",
                UseSsl = true,
                UseWebSocket = false
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("https://host/"));
            Assert.That(info.Connection, Is.EqualTo("https://host/"));



            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("wss://host:2/"));
            Assert.That(info.Connection, Is.EqualTo("wss://host:2/"));

            info = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = false,
                UseWebSocket = true
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("ws://host:2/"));
            Assert.That(info.Connection, Is.EqualTo("ws://host:2/"));

            info = new HostInfo
            {
                Host = "host",
                UseSsl = false,
                UseWebSocket = true,
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("ws://host/"));
            Assert.That(info.Connection, Is.EqualTo("ws://host/"));


            info = new HostInfo
            {
                Host = "host",
                UseSsl = true,
                UseWebSocket = true
            };

            Assert.That(info.BuildConnection(), Is.EqualTo("wss://host/"));
            Assert.That(info.Connection, Is.EqualTo("wss://host/"));
        }

        [Test]
        public void ValidateHostOnlyTest()
        {
            var info = new HostInfo { Host = "http://host" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "http://host/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "https://host" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "https://host/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "ws://host" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "ws://host/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "wss://host" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "wss://host/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));
        }

        [Test]
        public void ValidateHostAndPortTest()
        {
            var info = new HostInfo { Host = "http://host:20" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "http://host:20/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "https://host:20" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "https://host:20/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "ws://host:20" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "ws://host:20/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "wss://host:20" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "wss://host:20/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo(""));
        }

        [Test]
        public void ValidateHostAndPortAndPathTest()
        {
            var info = new HostInfo { Host = "http://host:20/path" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));


            info = new HostInfo { Host = "http://host:20/path/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));

            info = new HostInfo { Host = "https://host:20/path" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));


            info = new HostInfo { Host = "https://host:20/path/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));

            info = new HostInfo { Host = "ws://host:20/path" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));


            info = new HostInfo { Host = "ws://host:20/path/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));

            info = new HostInfo { Host = "wss://host:20/path" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));


            info = new HostInfo { Host = "wss://host:20/path/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(true));
            Assert.That(info.Port, Is.EqualTo(20));
            Assert.That(info.Path, Is.EqualTo("path"));
        }

        [Test]
        public void ValidateHostWrongFormatTest()
        {
            var info = new HostInfo { Host = "https://host:dada/" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "https://host:" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(true));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));


            info = new HostInfo { Host = "http://host" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo(""));

            info = new HostInfo { Host = "http://host", Path = "name" };
            info.ValidateHost();

            Assert.That(info.Host, Is.EqualTo("host"));
            Assert.That(info.UseSsl, Is.EqualTo(false));
            Assert.That(info.UseWebSocket, Is.EqualTo(false));
            Assert.That(info.Port, Is.EqualTo(null));
            Assert.That(info.Path, Is.EqualTo("name"));
        }

        [Test]
        public void IsTest()
        {
            var info = new HostInfo
            {
                Host = "1",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            ClassicAssert.True(info.Is(info.Clone()));
            ClassicAssert.False(info.Is(info.With(connectionInfo => connectionInfo.Host = "2")));
            ClassicAssert.False(info.Is(info.With(connectionInfo => connectionInfo.Port = 3)));
            ClassicAssert.False(info.Is(info.With(connectionInfo => connectionInfo.UseSsl = false)));
            ClassicAssert.False(info.Is(info.With(connectionInfo => connectionInfo.UseWebSocket = false)));
            ClassicAssert.False(info.Is(info.With(connectionInfo => connectionInfo.Path = "5")));
        }

        [Test]
        public void CloneTest()
        {
            var info1 = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            var info2 = info1.Clone() as HostInfo;
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);

            Assert.That(info2.Host, Is.EqualTo("host"));
            Assert.That(info2.Port, Is.EqualTo(2));
            Assert.That(info2.UseSsl, Is.EqualTo(true));
            Assert.That(info2.UseWebSocket, Is.EqualTo(true));
            Assert.That(info2.Path, Is.EqualTo("name"));
        }

        [Test]
        public void MessagePackSerializationTest()
        {
            var info1 = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            var bytes = info1.ToPackBytes();

            ClassicAssert.NotNull(bytes);

            var info2 = bytes.FromPackBytes<HostInfo>();
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);
            ClassicAssert.True(info1.Is(info2));


            info1 = new HostInfo
            {
                Host = "host",
                UseSsl = true,
                UseWebSocket = false
            };

            bytes = info1.ToPackBytes();

            ClassicAssert.NotNull(bytes);

            info2 = bytes.FromPackBytes<HostInfo>();
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);
            ClassicAssert.True(info1.Is(info2));
        }

        [Test]
        public void JsonSerializationTest()
        {
            var info1 = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            var bytes = info1.ToJsonBytes();

            ClassicAssert.NotNull(bytes);

            var info2 = bytes.FromJsonBytes<HostInfo>();
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);
            ClassicAssert.True(info1.Is(info2));
        }

        [Test]
        public void MemoryPackSerializationTest()
        {
            var info1 = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            var bytes = info1.ToMemoryPackBytes();

            ClassicAssert.NotNull(bytes);

            var info2 = bytes.FromMemoryPackBytes<HostInfo>();
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);
            ClassicAssert.True(info1.Is(info2));
        }

        [Test]
        public void ProtoBufSerializationTest()
        {
            var info1 = new HostInfo
            {
                Host = "host",
                Port = 2,
                UseSsl = true,
                UseWebSocket = true,
                Path = "name"
            };

            var bytes = info1.ToProtoBytes();

            ClassicAssert.NotNull(bytes);

            var info2 = bytes.FromProtoBytes<HostInfo>();
            ClassicAssert.NotNull(info2);

            ClassicAssert.AreNotSame(info1, info2);
            ClassicAssert.True(info1.Is(info2));
        }
    }
}
