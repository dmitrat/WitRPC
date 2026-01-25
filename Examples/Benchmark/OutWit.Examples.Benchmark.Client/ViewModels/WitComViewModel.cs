using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CsvHelper;
using Microsoft.Win32;
using OutWit.Common.Aspects;
using OutWit.Common.Collections;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Common.Utils;
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF.Utils;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Examples.Benchmark.Client.Model;
using OutWit.Examples.Benchmark.Client.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Contracts.Utils;

namespace OutWit.Examples.Benchmark.Client.ViewModels
{
    public class WitRPCViewModel: ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_MEMORY_MAPPED_FILE_NAME = "mmf";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_WEB_SOCKET_PATH = "api";

        private const int DEFAULT_TCP_PORT = 7001;

        private const int DEFAULT_WEB_SOCKET_PORT = 7001;

        private const int DEFAULT_BENCHMARK_ATTEMPTS = 50;

        private const long DEFAULT_DATA_SIZE = 10_000_00;


        private const string DEFAULT_HOST = "rdc.waveslogic.com";

        #endregion

        #region Constructors

        public WitRPCViewModel(ApplicationViewModel applicationVm) 
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
            BenchmarkResults = new ObservableCollection<BenchmarkResult>();

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

            BenchmarkAttempts = DEFAULT_BENCHMARK_ATTEMPTS;
            DataSize = DEFAULT_DATA_SIZE;


            Host = DEFAULT_HOST;

            CanConnectClient = true;
            CanDisconnectClient = false;
            Service = null;

            UpdateStatus();
        }

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            ConnectClientCmd = new RelayCommand(x => ConnectClient());
            DisconnectClientCmd = new RelayCommand(x => DisconnectClient());
            OneWayBenchmarkCmd = new RelayCommand(x => OneWayBenchmark());
            TwoWaysBenchmarkCmd = new RelayCommand(x => TwoWaysBenchmark());
            ExportCsvCmd = new RelayCommand(x => ExportCsv());
        }

        #endregion

        #region Functions

        private async void ConnectClient()
        {
            if(!CanConnectClient)
                return;

            var options = new WitClientBuilderOptions();
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
                        options.WithMemoryMappedFile(MemoryMappedFileName);
                    break;

                case WitRPCTransportType.NamedPipe:
                    if (!string.IsNullOrEmpty(PipeName))
                        options.WithNamedPipe(PipeName);
                    break;

                case WitRPCTransportType.TCP:
                    options.WithTcp($"{Host}", TcpPort);
                    break;


                case WitRPCTransportType.WebSocket:
                    options.WithWebSocket($"wss://{Host}:{WebSocketPort}/{WebSocketPath}");
                    break;
            }

            if(options.Transport == null)
                return;

            Client = WitClientBuilder.Build(options);
            var result = await Client.ConnectAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

            if (!result)
                return;

            Service = Client.GetService<IBenchmarkService>();

            CanConnectClient = false;
            CanDisconnectClient = true;
        }

        private void DisconnectClient()
        {
            if(!CanDisconnectClient)
                return;

            Client?.Disconnect();
            Client = null;

            CanConnectClient = true;
            CanDisconnectClient = false;
        }

        private async void OneWayBenchmark()
        {
            if(CanConnectClient || Service == null || BenchmarkAttempts <= 0)
                return;

            BenchmarkResults.Clear();

            var id = BenchmarkUtils.NextId();

            var data = BenchmarkUtils.GenerateData(DataSize);
            var hash = HashUtils.FastHash(data);

            for (int i = 0; i < BenchmarkAttempts; i++)
            {
                try
                {
                    var start = DateTime.Now;

                    var result = await Service.OneWayBenchmark(data);

                    var end = DateTime.Now;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BenchmarkResults.Add(new BenchmarkResult
                        {
                            SeriesId = id,
                            Name = "OneWayBenchmark",
                            Type = "WitRPC",
                            Transport = $"{TransportType}",
                            Serializer = $"{SerializerType}",
                            UseEncryption = UseEncryption,
                            UseAuthorization = UseAuthorization,
                            Duration = (end - start),
                            DurationMs = (end - start).TotalMilliseconds,
                            Success = result == hash
                        });
                    });
                    
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {

                }
            }

           

        }

        private async void TwoWaysBenchmark()
        {
            if (CanConnectClient || Service == null || BenchmarkAttempts <= 0)
                return;

            BenchmarkResults.Clear();

            var id = BenchmarkUtils.NextId();

            var data = BenchmarkUtils.GenerateData(DataSize);

            for (int i = 0; i < BenchmarkAttempts; i++)
            {
                try
                {
                    var start = DateTime.Now;

                    var result = await Service.TwoWaysBenchmark(data);

                    var end = DateTime.Now;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BenchmarkResults.Add(new BenchmarkResult
                        {
                            SeriesId = id,
                            Name = "TwoWaysBenchmark",
                            Type = "WitRPC",
                            Transport = $"{TransportType}",
                            Serializer = $"{SerializerType}",
                            UseEncryption = UseEncryption,
                            UseAuthorization = UseAuthorization,
                            Duration = (end - start),
                            DurationMs = (end - start).TotalMilliseconds,
                            Success = data.Is(result)
                        });
                    });

                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    

                }
       
            }
        }

        public void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = BuildFileName()
            };

            if(dialog.ShowDialog() != true)
                return;

            using (var writer = new StreamWriter(dialog.FileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                csv.WriteRecords(BenchmarkResults);
            
        }

        private string BuildFileName()
        {
            var fileName = $"WitRPC_{TransportType}_{SerializerType}_{BenchmarkResults.FirstOrDefault()?.Name}";
            if (UseEncryption)
                fileName = $"{fileName}_Encryption";
            if (UseAuthorization)
                fileName = $"{fileName}_Authorization";
            return $"{fileName}.csv";
            
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
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; private set; } = null!;

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
        public bool CanConnectClient { get; private set; }

        [Notify]
        public bool CanDisconnectClient { get; private set; }

        [Notify]
        public bool IsMemoryMappedFile { get; private set; }

        [Notify]
        public bool IsNamedPipe { get; private set; }

        [Notify]
        public bool IsTcp { get; private set; }

        [Notify]
        public bool IsWebSocket { get; private set; }

        [Notify]
        public int BenchmarkAttempts { get; set; }

        [Notify]
        public long DataSize { get; set; }


        [Notify]
        public string Host { get; set; }

        private WitClient? Client { get; set; }

        private IBenchmarkService? Service { get; set; }

        #endregion

        #region Commands

        [Notify] 
        public ICommand ConnectClientCmd { get; private set; } = null!;

        [Notify]
        public ICommand DisconnectClientCmd { get; private set; } = null!;

        [Notify]
        public ICommand OneWayBenchmarkCmd { get; private set; } = null!;

        [Notify]
        public ICommand TwoWaysBenchmarkCmd { get; private set; } = null!;

        [Notify]
        public ICommand ExportCsvCmd { get; private set; } = null!;

        #endregion
    }
}
