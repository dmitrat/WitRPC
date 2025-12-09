using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using OutWit.Communication.Client.Authorization;
using OutWit.Communication.Client.Encryption;
using OutWit.Communication.Client.Pipes;
using OutWit.Communication.Client;
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

namespace OutWit.Communication.Tests
{
    public static class Shared
    {
        private const string AUTHORIZATION_TOKEN = "token";

        public static IServiceBase GetServiceStatic(WitClient client)
        {
            var interceptor = new RequestInterceptorDynamic(client, true);

            return new ServiceProxy(interceptor);
        }

        public static IService GetServiceDynamic(WitClient client)
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(client, true);

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<IService>(interceptor);
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

        public static WitClient GetClient(TransportType transportType, SerializerType serializerType, string testName)
        {
            return new WitClient(GetClientTransport(transportType, testName),
                new EncryptorClientGeneral(),
                new AccessTokenProviderStatic(AUTHORIZATION_TOKEN),
                GetSerializer(serializerType), 
                new MessageSerializerMemoryPack(),
                null, null);
        }


        private static ITransportServerFactory GetServerTransport(TransportType transportType, int maxNumberOfClients, string name)
        {
            switch (transportType)
            {
                case TransportType.MMF:
                    return new MemoryMappedFileServerTransportFactory(new MemoryMappedFileServerTransportOptions()
                    {
                        Name = name,
                        Size = 1024 * 1024
                    });

                case TransportType.Pipes:
                    return new NamedPipeServerTransportFactory(new NamedPipeServerTransportOptions
                    {
                        PipeName = name,
                        MaxNumberOfClients = maxNumberOfClients
                    });

                case TransportType.Tcp:
                    {
                        var random = new Random(name.GetHashCode());

                        return new TcpServerTransportFactory(new TcpServerTransportOptions
                        {
                            Port = random.Next(100, 300),
                            MaxNumberOfClients = maxNumberOfClients
                        });
                    }

                case TransportType.TcpSecure:
                    {
                        var random = new Random(name.GetHashCode());
                        return new TcpSecureServerTransportFactory(new TcpSecureServerTransportOptions
                        {
                            Port = random.Next(100, 300),
                            MaxNumberOfClients = maxNumberOfClients,
                            Certificate = new X509Certificate(Properties.Resources.certificate1, "Pa$$w0rd")
                        });
                    }

                case TransportType.WebSocket:
                default:
                    return new WebSocketServerTransportFactory(new WebSocketServerTransportOptions
                    {
                        Host = (HostInfo?)$"http://localhost:5000/{name}/",
                        MaxNumberOfClients = maxNumberOfClients,
                        BufferSize = 1024 * 1024
                    });
            }
        }

        private static ITransportClient GetClientTransport(TransportType transportType, string name)
        {
            switch (transportType)
            {
                case TransportType.MMF:
                    return new MemoryMappedFileClientTransport(new MemoryMappedFileClientTransportOptions()
                    {
                        Name = name
                    });

                case TransportType.Pipes:
                    return new NamedPipeClientTransport(new NamedPipeClientTransportOptions
                    {
                        ServerName = ".",
                        PipeName = name
                    });

                case TransportType.Tcp:
                    {
                        var random = new Random(name.GetHashCode());
                        return new TcpClientTransport(new TcpClientTransportOptions
                        {
                            Port = random.Next(100, 300),
                            Host = "127.0.0.1"
                        });
                    }

                case TransportType.TcpSecure:
                    {
                        var random = new Random(name.GetHashCode());
                        return new TcpSecureClientTransport(new TcpSecureClientTransportOptions
                        {
                            Port = random.Next(100, 300),
                            Host = "127.0.0.1",
                            TargetHost = "localhost",
                            SslValidationCallback = AcceptAllCertificates
                        });
                    }

                case TransportType.WebSocket:
                default:
                    return new WebSocketClientTransport(new WebSocketClientTransportOptions
                    {
                        Url = $"ws://localhost:5000/{name}/",
                        BufferSize = 1024 * 1024
                    });
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
