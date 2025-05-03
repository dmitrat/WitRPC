using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CsvHelper;
using Grpc.Net.Client;
using Microsoft.Win32;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.Collections;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Communication.Client;
using OutWit.Communication.Client.MMF.Utils;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Client.Tcp.Utils;
using OutWit.Examples.Benchmark.Client.Model;
using OutWit.Examples.Benchmark.Client.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Contracts.Model;
using OutWit.Examples.Contracts.Utils;
using ProtoBuf.Grpc.Client;

namespace OutWit.Examples.Benchmark.Client.ViewModels
{
    public class GRPCViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_HTTP_PORT = 8082;

        private const int DEFAULT_BENCHMARK_ATTEMPTS = 50;

        private const long DEFAULT_DATA_SIZE = 10_000_000;

        private const string DEFAULT_HOST = "localhost";
        
        #endregion

        #region Constructors

        public GRPCViewModel(ApplicationViewModel applicationVm) 
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
                GRPCTransportType.HTTP,
                GRPCTransportType.NamedPipe,
            ];

            TransportType = GRPCTransportType.HTTP;

            SerializerTypes =
            [
                GRPCSerializerType.ProtoBuf,
            ];

            SerializerType = GRPCSerializerType.ProtoBuf;

            UseEncryption = true;
            UseAuthorization = true;
            AuthorizationToken = DEFAULT_TOKEN;

            PipeName = DEFAULT_PIPE_NAME;
            HttpPort = DEFAULT_HTTP_PORT;
            HttpPath = DEFAULT_HTTP_PATH;

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
            ConnectClientCmd = new DelegateCommand(x => ConnectClient());
            DisconnectClientCmd = new DelegateCommand(x => DisconnectClient());
            OneWayBenchmarkCmd = new DelegateCommand(x => OneWayBenchmark());
            TwoWaysBenchmarkCmd = new DelegateCommand(x => TwoWaysBenchmark());
            ExportCsvCmd = new DelegateCommand(x => ExportCsv());
        }

        #endregion

        #region Functions

        private async void ConnectClient()
        {
            if(!CanConnectClient)
                return;

            try
            {
                if (IsNamedPipe)
                {
                    var handler = new SocketsHttpHandler
                    {
                        ConnectCallback = async (ctx, ct) =>
                        {
                            var pipe = new NamedPipeClientStream(".", PipeName!, PipeDirection.InOut, PipeOptions.Asynchronous);
                            await pipe.ConnectAsync(ct);
                            return pipe;
                        }
                    };
                    Channel = GrpcChannel.ForAddress($"http://{Host}", new GrpcChannelOptions
                    {
                        HttpHandler = handler,
                        MaxReceiveMessageSize = 64 * 1024 * 1024,
                        MaxSendMessageSize = 64 * 1024 * 1024,
                    });
                }
                if(IsHttp)
                    Channel = GrpcChannel.ForAddress($"http://{Host}:{HttpPort}", new GrpcChannelOptions
                    {
                        MaxReceiveMessageSize = 64 * 1024 * 1024,
                        MaxSendMessageSize = 64 * 1024 * 1024,
                    });

                Service = Channel?.CreateGrpcService<IBenchmarkGrpcServiceProto>();

                CanConnectClient = false;
                CanDisconnectClient = true;
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }

           
        }

        private void DisconnectClient()
        {
            if(!CanDisconnectClient)
                return;

            if(Channel != null)
                Channel.Dispose();

            Service = null;

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

                    var result = await Service.OneWayBenchmark(new BenchmarkRequest
                    {
                        Bytes = data
                    });

                    var end = DateTime.Now;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BenchmarkResults.Add(new BenchmarkResult
                        {
                            SeriesId = id,
                            Name = "OneWayBenchmark",
                            Type = "WitCom",
                            Transport = $"{TransportType}",
                            Serializer = $"{SerializerType}",
                            UseEncryption = UseEncryption,
                            UseAuthorization = UseAuthorization,
                            Duration = (end - start),
                            DurationMs = (end - start).TotalMilliseconds,
                            Success = result.Length == hash
                        });
                    });
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

                    var result = await Service.TwoWaysBenchmark(new BenchmarkRequest
                    {
                        Bytes = data
                    });

                    var end = DateTime.Now;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BenchmarkResults.Add(new BenchmarkResult
                        {
                            SeriesId = id,
                            Name = "TwoWaysBenchmark",
                            Type = "WitCom",
                            Transport = $"{TransportType}",
                            Serializer = $"{SerializerType}",
                            UseEncryption = UseEncryption,
                            UseAuthorization = UseAuthorization,
                            Duration = (end - start),
                            DurationMs = (end - start).TotalMilliseconds,
                            Success = data.Is(result.Bytes)
                        });
                    });
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
            var fileName = $"gRPC_{TransportType}_{SerializerType}_{BenchmarkResults.FirstOrDefault()?.Name}";
            return $"{fileName}.csv";

        }
        
        private void UpdateStatus()
        {
            IsNamedPipe = TransportType == GRPCTransportType.NamedPipe;
            IsHttp = TransportType == GRPCTransportType.HTTP;
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
        public IReadOnlyList<GRPCTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public GRPCTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<GRPCSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public GRPCSerializerType? SerializerType { get; set; }

        [Notify]
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; private set; } = null!;
        
        [Notify]
        public string? PipeName { get; set; }

        [Notify]
        public int HttpPort { get; set; }

        [Notify]
        public string? HttpPath { get; set; }

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
        public bool IsNamedPipe { get; private set; }

        [Notify]
        public bool IsHttp { get; private set; }

        [Notify]
        public int BenchmarkAttempts { get; set; }


        [Notify]
        public string Host { get; set; }

        [Notify]
        public long DataSize { get; set; }

        private GrpcChannel? Channel { get; set; }

        private IBenchmarkGrpcServiceProto? Service { get; set; }

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
