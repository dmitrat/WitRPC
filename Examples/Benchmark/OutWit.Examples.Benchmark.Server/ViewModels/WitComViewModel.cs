using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Common.Utils;
using OutWit.Communication.Server;
using OutWit.Communication.Server.MMF.Utils;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Examples.Benchmark.Server.Model;
using OutWit.Examples.Benchmark.Server.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;

namespace OutWit.Examples.Benchmark.Server.ViewModels
{
    public class WitRPCViewModel: ViewModelBase<ApplicationViewModel>
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

        public WitRPCViewModel(ApplicationViewModel applicationVm) 
            : base(applicationVm)
        {
            InitDefaults();
            InitEvents();
            InitCommands();
            InitLog();
        }

        #endregion

        #region Inititalization

        private void InitDefaults()
        {
            Service = new BenchmarkService();

            TransportTypes =
            [
                WitRPCTransportType.MemoryMappedFile,
                WitRPCTransportType.NamedPipe,
                WitRPCTransportType.TCP,
                WitRPCTransportType.WebSocket
            ];

            TransportType = WitRPCTransportType.MemoryMappedFile;

            SerializerTypes =
            [
                WitRPCSerializerType.Json,
                WitRPCSerializerType.MessagePack,
                WitRPCSerializerType.MemoryPack,
                WitRPCSerializerType.ProtoBuf
            ];

            SerializerType = WitRPCSerializerType.Json;

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
            StartServerCmd = new RelayCommand(x => StartServer());
            StopServerCmd = new RelayCommand(x => StopServer());
            ResetMemoryMappedFileNameCmd = new RelayCommand(x => ResetMemoryMappedFileName());
            ResetPipeNameCmd = new RelayCommand(x => ResetPipeName());
            ResetWebSocketPathCmd = new RelayCommand(x => ResetWebSocketPath());
            ResetWebSocketPortCmd = new RelayCommand(x => ResetWebSocketPort());
            ResetTcpPortCmd = new RelayCommand(x => ResetTcpPort());
            ResetAuthorizationTokenCmd = new RelayCommand(x => ResetAuthorizationToken());
        }

        private void InitLog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .Enrich.WithExceptionDetails()
                .WriteTo.File(Path.Combine(Application.ResourceAssembly.ApplicationDataPath(2, "Log"), "log.txt"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true, fileSizeLimitBytes: 524288)
                .CreateLogger();

            LoggerFactory = new SerilogLoggerFactory();
        }

        #endregion

        #region Functions

        private void StartServer()
        {
            if(!CanStartServer)
                return;

            var options = new WitServerBuilderOptions();
            options.WithService(Service);

            options.WithLogger(LoggerFactory.CreateLogger("WitRPC"));
            
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
                case WitRPCSerializerType.Json:
                    options.WithJson();
                    break;

                case WitRPCSerializerType.MessagePack:
                    options.WithMessagePack();
                    break;


                case WitRPCSerializerType.MemoryPack:
                    options.WithMemoryPack();
                    break;

                case WitRPCSerializerType.ProtoBuf:
                    options.WithProtoBuf();
                    break;
            }

            switch (TransportType)
            {
                case WitRPCTransportType.MemoryMappedFile:
                    if(!string.IsNullOrEmpty(MemoryMappedFileName))
                        options.WithMemoryMappedFile(MemoryMappedFileName, 10_000_000_000);
                    break;

                case WitRPCTransportType.NamedPipe:
                    if (!string.IsNullOrEmpty(PipeName))
                        options.WithNamedPipe(PipeName, 10);
                    break;

                case WitRPCTransportType.TCP:
                    options.WithTcp(TcpPort, 10);
                    break;


                case WitRPCTransportType.WebSocket:
                    options.WithWebSocket($"http://127.0.0.1:{WebSocketPort}/{WebSocketPath}", 10);
                    break;
            }

            if(options.TransportFactory == null)
                return;

            Server = WitServerBuilder.Build(options);
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
            IsMemoryMappedFile = TransportType == WitRPCTransportType.MemoryMappedFile;
            IsNamedPipe = TransportType == WitRPCTransportType.NamedPipe;
            IsTcp = TransportType == WitRPCTransportType.TCP;
            IsWebSocket = TransportType == WitRPCTransportType.WebSocket;
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.IsProperty((WitRPCViewModel vm)=>vm.TransportType))
                UpdateStatus();
        }

        #endregion

        #region Properties

        [Notify] 
        public IReadOnlyList<WitRPCTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public WitRPCTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<WitRPCSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public WitRPCSerializerType? SerializerType { get; set; }

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

        private WitServer? Server { get; set; }

        private IBenchmarkService Service { get; set; } = null!;

        public Microsoft.Extensions.Logging.ILoggerFactory LoggerFactory { get; private set; }

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
