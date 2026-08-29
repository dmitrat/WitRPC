using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Pipes;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Reconnection;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Authorization;
using OutWit.Communication.Server.Discovery;
using OutWit.Communication.Server.Encryption;
using OutWit.Communication.Server.Pipes;
using OutWit.Communication.Server;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Server.MMF;
using OutWit.Communication.Server.Tcp;
using OutWit.Communication.Model;
using OutWit.Communication.Server.WebSocket;
using OutWit.Communication.Client.MMF;
using OutWit.Communication.Client.Tcp;
using OutWit.Communication.Client.WebSocket;
using OutWit.Communication.Processors;
using OutWit.Communication.Tests.Mock.Interfaces;
using Castle.DynamicProxy;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Tests._Mock.Interfaces;
using OutWit.Communication.Client.Encryption.BouncyCastle;
using OutWit.Communication.Server.Encryption.BouncyCastle;

namespace OutWit.Communication.Tests
{
    public static class Shared
    {
        private const string AUTHORIZATION_TOKEN = "token";

        private static readonly object PORT_LOCK = new object();

        private static readonly Dictionary<string, int> PORTS_BY_KEY = new Dictionary<string, int>();

        /// <summary>
        /// Kernel-object channel names (MMF, pipes) carry the process id: a
        /// multi-TFM `dotnet test` runs one test host per TFM, and two hosts
        /// running the same-named test must not collide on the same channel.
        /// Within a process the suffix is stable, so restart tests still reuse
        /// their channel.
        /// </summary>
        internal static string ChannelName(string name)
        {
            return $"{name}_{Environment.ProcessId}";
        }

        /// <summary>
        /// One port per (transport, test name), chosen by the OS. Binding port 0
        /// hands back a port that is actually free and assignable -- unlike a
        /// port drawn from a fixed range, it can never land in a Windows
        /// excluded-port block (the source of the intermittent AccessDenied /
        /// 10013 bind failures). The server and client sides of a test share
        /// the port through this cache, and a restart test reuses its port.
        /// Ports are process-local, so parallel TFM hosts cannot collide here.
        /// </summary>
        private static int PortFor(string key)
        {
            lock (PORT_LOCK)
            {
                if (PORTS_BY_KEY.TryGetValue(key, out int port))
                    return port;

                var probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();

                PORTS_BY_KEY[key] = port;
                return port;
            }
        }

        public static IServiceBase GetServiceStatic(WitClient client)
        {
            var interceptor = new RequestInterceptorDynamic(client, true);

            return new ServiceProxy(interceptor);
        }

        public static IService GetService(WitClient client)
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(client, true);

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<IService>(interceptor);
        }

        public static IService GetServiceDynamic(WitClient client)
        {
            return GetService(client);
        }

        public static WitServer GetServer(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName)
        {
            var service = new MockService();
            
            return new WitServer(GetServerTransport(transportType, maxNumberOfClients, testName),
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                new RequestProcessor<IService>(service),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        public static WitServer GetServerWithBouncyCastle(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName)
        {
            var service = new MockService();
            
            return new WitServer(GetServerTransport(transportType, maxNumberOfClients, testName),
                new EncryptorServerBouncyCastleFactory(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                new RequestProcessor<IService>(service),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        public static WitServer GetServerWithCompositeServices(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName)
        {
            var processor = new CompositeRequestProcessor()
                .Register<ITestChannel1>(new TestChannel1Impl())
                .Register<ITestChannel2>(new TestChannel2Impl());

            return new WitServer(GetServerTransport(transportType, maxNumberOfClients, testName),
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                processor,
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        public static WitServer GetServerBasic(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName)
        {
            return new WitServer(GetServerTransport(transportType, maxNumberOfClients, testName),
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                new MockRequestProcessor(),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null);
        }

        public static WitServer GetServerBasicWithHandshakeTimeout(TransportType transportType, SerializerType serializerType, int maxNumberOfClients, string testName, TimeSpan handshakeTimeout)
        {
            return new WitServer(GetServerTransport(transportType, maxNumberOfClients, testName),
                new EncryptorServerFactory<EncryptorServerGeneral>(),
                new AccessTokenValidatorStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                new MockRequestProcessor(),
                new DiscoveryServer(new DiscoveryServerOptions
                {
                    IpAddress = IPAddress.Parse("239.255.255.250"),
                    Port = 3702,
                    Mode = DiscoveryServerMode.StartStop
                }),
                null, null, null, null, int.MaxValue, handshakeTimeout);
        }

        public static WitClient GetClient(TransportType transportType, SerializerType serializerType, string testName)
        {
            return new WitClient(GetClientTransport(transportType, testName),
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                null, null);
        }

        public static WitClient GetClientWithToken(TransportType transportType, SerializerType serializerType, string testName, string token)
        {
            return new WitClient(GetClientTransport(transportType, testName),
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(token),
                GetSerializer(serializerType),
                new MessageSerializerMemoryPack(),
                null, null);
        }

        public static WitClient GetClientWithBouncyCastle(TransportType transportType, SerializerType serializerType, string testName)
        {
            return new WitClient(GetClientTransport(transportType, testName),
                new EncryptorClientBouncyCastle(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType), 
                new MessageSerializerMemoryPack(),
                null, null);
        }

        /// <summary>
        /// Creates a client with auto-reconnection options.
        /// </summary>
        public static WitClient GetClientWithReconnection(TransportType transportType, SerializerType serializerType, 
            string testName, Action<ReconnectionOptions> configureReconnection)
        {
            var reconnectionOptions = new ReconnectionOptions();
            configureReconnection(reconnectionOptions);

            return new WitClient(GetClientTransport(transportType, testName),
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType), 
                new MessageSerializerMemoryPack(),
                reconnectionOptions,
                null, null);
        }


        public static ITransportServerFactory GetServerTransport(TransportType transportType, int maxNumberOfClients, string name)
        {
            switch (transportType)
            {
                case TransportType.MMF:
                    return new MemoryMappedFileServerTransportFactory(new MemoryMappedFileServerTransportOptions()
                    {
                        Name = ChannelName(name),
                        Size = 1024 * 1024
                    });

                case TransportType.Pipes:
                    return new NamedPipeServerTransportFactory(new NamedPipeServerTransportOptions
                    {
                        PipeName = ChannelName(name),
                        MaxNumberOfClients = maxNumberOfClients
                    });

                case TransportType.Tcp:
                    return new TcpServerTransportFactory(new TcpServerTransportOptions
                    {
                        Port = PortFor($"Tcp:{name}"),
                        MaxNumberOfClients = maxNumberOfClients
                    });

                case TransportType.TcpSecure:
                    {
                        return new TcpSecureServerTransportFactory(new TcpSecureServerTransportOptions
                        {
                            Port = PortFor($"TcpSecure:{name}"),
                            MaxNumberOfClients = maxNumberOfClients,
#if NET9_0_OR_GREATER
                            Certificate = X509CertificateLoader.LoadPkcs12(Properties.Resources.certificate1, "Pa$$w0rd")
#else
                            Certificate = new X509Certificate(Properties.Resources.certificate1, "Pa$$w0rd")
#endif
                        });
                    }

                case TransportType.WebSocket:
                default:
                    {
                        var port = PortFor($"WebSocket:{name}");

                        return new WebSocketServerTransportFactory(new WebSocketServerTransportOptions
                        {
                            Host = (HostInfo?)$"http://localhost:{port}/{name}/",
                            MaxNumberOfClients = maxNumberOfClients,
                            BufferSize = 1024 * 1024
                        });
                    }
            }
        }

        public static ITransportClient GetClientTransport(TransportType transportType, string name)
        {
            switch (transportType)
            {
                case TransportType.MMF:
                    return new MemoryMappedFileClientTransport(new MemoryMappedFileClientTransportOptions()
                    {
                        Name = ChannelName(name)
                    });

                case TransportType.Pipes:
                    return new NamedPipeClientTransport(new NamedPipeClientTransportOptions
                    {
                        ServerName = ".",
                        PipeName = ChannelName(name)
                    });

                case TransportType.Tcp:
                    return new TcpClientTransport(new TcpClientTransportOptions
                    {
                        Port = PortFor($"Tcp:{name}"),
                        Host = "127.0.0.1"
                    });

                case TransportType.TcpSecure:
                    {
                        return new TcpSecureClientTransport(new TcpSecureClientTransportOptions
                        {
                            Port = PortFor($"TcpSecure:{name}"),
                            Host = "127.0.0.1",
                            TargetHost = "localhost",
                            SslValidationCallback = AcceptAllCertificates
                        });
                    }

                case TransportType.WebSocket:
                default:
                    {
                        var port = PortFor($"WebSocket:{name}");

                        return new WebSocketClientTransport(new WebSocketClientTransportOptions
                        {
                            Url = $"ws://localhost:{port}/{name}/",
                            BufferSize = 1024 * 1024
                        });
                    }
            }
        }

        /// <summary>
        /// SSL validation callback that accepts all certificates. 
        /// Used only for testing with self-signed certificates.
        /// </summary>
        private static bool AcceptAllCertificates(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static IMessageSerializer GetSerializer(SerializerType serializerType)
        {
            switch (serializerType)
            {
                case SerializerType.Json:
                    return new MessageSerializerJson();

                case SerializerType.MessagePack:
                    return new MessageSerializerMessagePack();

                case SerializerType.MemoryPack:
                    return new MessageSerializerMemoryPack();

                case SerializerType.ProtoBuf:
                    return new MessageSerializerProtoBuf();

                default:
                    return new MessageSerializerJson();
            }
        }
    }

    public enum TransportType
    {
        MMF,
        Pipes,
        Tcp,
        TcpSecure,
        WebSocket
    }

    public enum SerializerType
    {
        Json,
        MessagePack,
        MemoryPack,
        ProtoBuf
    }
}
