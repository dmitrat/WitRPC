using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Input;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.MMF.Utils;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Examples.Benchmark.Server.Model;
using OutWit.Examples.Benchmark.Server.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services;

namespace OutWit.Examples.Benchmark.Server.ViewModels
{
    public class WitComViewModel: ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_MEMORY_MAPPED_FILE_NAME = "mmf";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_WEB_SOCKET_PATH = "api";

        private const int DEFAULT_TCP_PORT = 8081;

        private const int DEFAULT_WEB_SOCKET_PORT = 8082;

        #endregion

        #region Constructors

        public WitComViewModel(ApplicationViewModel applicationVm) 
            : base(applicationVm)
        {
            InitDefaults();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Inititalization

        private void InitDefaults()
        {
            Service = new BenchmarkService();

            TransportTypes =
            [
                WitComTransportType.MemoryMappedFile,
                WitComTransportType.NamedPipe,
                WitComTransportType.TCP,
                WitComTransportType.WebSocket
            ];

            TransportType = WitComTransportType.MemoryMappedFile;

            SerializerTypes =
            [
                WitComSerializerType.Json,
                WitComSerializerType.MessagePack,
                WitComSerializerType.MemoryPack,
                WitComSerializerType.ProtoBuf
            ];

            SerializerType = WitComSerializerType.Json;

            UseEncryption = true;
            UseAuthorization = true;
            AuthorizationToken = DEFAULT_TOKEN;

            MemoryMappedFileName = DEFAULT_MEMORY_MAPPED_FILE_NAME;
            PipeName = DEFAULT_PIPE_NAME;
            TcpPort = DEFAULT_TCP_PORT;
            WebSocketPort = DEFAULT_WEB_SOCKET_PORT;
            WebSocketPath = DEFAULT_WEB_SOCKET_PATH;

            CanStartServer = true;
            CanStopServer = false;
            Server = null;

            UpdateStatus();
        }

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            StartServerCmd = new DelegateCommand(x => StartServer());
            StopServerCmd = new DelegateCommand(x => StopServer());
            ResetMemoryMappedFileNameCmd = new DelegateCommand(x => ResetMemoryMappedFileName());
            ResetPipeNameCmd = new DelegateCommand(x => ResetPipeName());
            ResetWebSocketPathCmd = new DelegateCommand(x => ResetWebSocketPath());
            ResetWebSocketPortCmd = new DelegateCommand(x => ResetWebSocketPort());
            ResetTcpPortCmd = new DelegateCommand(x => ResetTcpPort());
            ResetAuthorizationTokenCmd = new DelegateCommand(x => ResetAuthorizationToken());
        }

        #endregion

        #region Functions

        private void StartServer()
        {
            if(!CanStartServer)
                return;

            var options = new WitComServerBuilderOptions();
            options.WithService(Service);
            if(UseEncryption)
                options.WithEncryption();
            else
                options.WithoutEncryption();

            if (UseAuthorization && !string.IsNullOrEmpty(AuthorizationToken))
                options.WithAccessToken(AuthorizationToken);
            else
                options.WithoutAuthorization();

            options.WithTimeout(TimeSpan.FromMinutes(1));

            switch (SerializerType)
            {
                case WitComSerializerType.Json:
                    options.WithJson();
                    break;

                case WitComSerializerType.MessagePack:
                    options.WithMessagePack();
                    break;


                case WitComSerializerType.MemoryPack:
                    options.WithMemoryPack();
                    break;

                case WitComSerializerType.ProtoBuf:
                    options.WithProtoBuf();
                    break;
            }

            switch (TransportType)
            {
                case WitComTransportType.MemoryMappedFile:
                    if(!string.IsNullOrEmpty(MemoryMappedFileName))
                        options.WithMemoryMappedFile(MemoryMappedFileName, 10_000_000_000);
                    break;

                case WitComTransportType.NamedPipe:
                    if (!string.IsNullOrEmpty(PipeName))
                        options.WithNamedPipe(PipeName, 10);
                    break;

                case WitComTransportType.TCP:
                    options.WithTcp(TcpPort, 10);
                    break;


                case WitComTransportType.WebSocket:
                    options.WithWebSocket($"http://localhost:{WebSocketPort}/{WebSocketPath}", 10);
                    break;
            }

            if(options.TransportFactory == null)
                return;

            Server = WitComServerBuilder.Build(options);
            Server.StartWaitingForConnection();

            CanStartServer = false;
            CanStopServer = true;
        }

        private void StopServer()
        {
            if(!CanStopServer)
                return;

            Server?.StopWaitingForConnection();
            Server?.Dispose();
            Server = null;

            CanStartServer = true;
            CanStopServer = false;
        }

        private void ResetMemoryMappedFileName()
        {
            MemoryMappedFileName = RandomUtils.RandomString();
        }

        private void ResetPipeName()
        {
            PipeName = RandomUtils.RandomString();
        }

        private void ResetWebSocketPath()
        {
            WebSocketPath = RandomUtils.RandomString();
        }

        private void ResetWebSocketPort()
        {
            WebSocketPort = NetworkUtils.NextFreePort();
        }

        private void ResetTcpPort()
        {
            TcpPort = NetworkUtils.NextFreePort();
        }

        private void ResetAuthorizationToken()
        {
            AuthorizationToken = RandomUtils.RandomString();
        }

        private void UpdateStatus()
        {
            IsMemoryMappedFile = TransportType == WitComTransportType.MemoryMappedFile;
            IsNamedPipe = TransportType == WitComTransportType.NamedPipe;
            IsTcp = TransportType == WitComTransportType.TCP;
            IsWebSocket = TransportType == WitComTransportType.WebSocket;
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.IsProperty((WitComViewModel vm)=>vm.TransportType))
                UpdateStatus();
        }

        #endregion

        #region Properties

        [Notify] 
        public IReadOnlyList<WitComTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public WitComTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<WitComSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public WitComSerializerType? SerializerType { get; set; }

        [Notify]
        public string? MemoryMappedFileName { get; set; }

        [Notify]
        public string? PipeName { get; set; }

        [Notify]
        public int TcpPort { get; set; }

        [Notify]
        public int WebSocketPort { get; set; }

        [Notify]
        public string? WebSocketPath { get; set; }

        [Notify]
        public bool UseEncryption { get; set; }

        [Notify]
        public bool UseAuthorization { get; set; }

        [Notify]
        public string? AuthorizationToken { get; set; }

        [Notify]
        public bool CanStartServer { get; private set; }

        [Notify]
        public bool CanStopServer { get; private set; }

        [Notify]
        public bool IsMemoryMappedFile { get; private set; }

        [Notify]
        public bool IsNamedPipe { get; private set; }

        [Notify]
        public bool IsTcp { get; private set; }

        [Notify]
        public bool IsWebSocket { get; private set; }

        private WitComServer? Server { get; set; }

        private IBenchmarkService Service { get; set; } = null!;

        #endregion

        #region Commands

        [Notify] 
        public ICommand StartServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand StopServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetMemoryMappedFileNameCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetPipeNameCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetWebSocketPathCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetWebSocketPortCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetTcpPortCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetAuthorizationTokenCmd { get; private set; } = null!;

        #endregion
    }
}
