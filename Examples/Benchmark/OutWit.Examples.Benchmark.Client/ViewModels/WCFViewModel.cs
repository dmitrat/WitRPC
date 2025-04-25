using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CsvHelper;
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
using OutWit.Examples.Contracts.Utils;

namespace OutWit.Examples.Benchmark.Client.ViewModels
{
    public class WCFViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_MEMORY_MAPPED_FILE_NAME = "mmf";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_TCP_PORT = 8081;

        private const int DEFAULT_HTTP_PORT = 8082;

        private const int DEFAULT_BENCHMARK_ATTEMPTS = 10;

        private const long DEFAULT_DATA_SIZE = 5_000_000;

        #endregion

        #region Constructors

        public WCFViewModel(ApplicationViewModel applicationVm) 
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
                WCFTransportType.NamedPipe,
                WCFTransportType.TCP,
                WCFTransportType.HTTP
            ];

            TransportType = WCFTransportType.HTTP;

            SerializerTypes =
            [
                WCFSerializerType.Json,
                WCFSerializerType.XML,
                WCFSerializerType.ProtoBuf
            ];

            SerializerType = WCFSerializerType.Json;

            UseEncryption = true;
            UseAuthorization = true;
            AuthorizationToken = DEFAULT_TOKEN;

            PipeName = DEFAULT_PIPE_NAME;
            TcpPort = DEFAULT_TCP_PORT;
            HttpPort = DEFAULT_HTTP_PORT;
            HttpPath = DEFAULT_HTTP_PATH;

            BenchmarkAttempts = DEFAULT_BENCHMARK_ATTEMPTS;
            DataSize = DEFAULT_DATA_SIZE;

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

            Binding? binding = null;
            EndpointAddress? address = null;

            switch (TransportType)
            {
                case WCFTransportType.HTTP:
                    binding = new BasicHttpBinding
                    {
                        MaxReceivedMessageSize = 10*1024*1024
                    };
                    address = new EndpointAddress($"http://localhost:{HttpPort}/{HttpPath}");
                    break;

                case WCFTransportType.TCP:
                    binding = new NetTcpBinding
                    {
                        MaxReceivedMessageSize = 10 * 1024 * 1024
                    };
                    address = new EndpointAddress($"net.tcp://localhost:{TcpPort}/{HttpPath}");
                    break;

                case WCFTransportType.NamedPipe:
                    binding = new NetNamedPipeBinding
                    {
                        MaxReceivedMessageSize = 10 * 1024 * 1024
                    };
                    address = new EndpointAddress($"net.pipe://localhost/{PipeName}");
                    break;

                default:
                    binding = new BasicHttpBinding
                    {
                        MaxReceivedMessageSize = 10 * 1024 * 1024
                    };
                    address = new EndpointAddress($"http://localhost:{HttpPort}/{HttpPath}");
                    break;
            }


            var factory = new ChannelFactory<IBenchmarkService>(binding, address);

            Service = factory.CreateChannel();
            Factory = factory;


            CanConnectClient = false;
            CanDisconnectClient = true;
        }

        private void DisconnectClient()
        {
            if(!CanDisconnectClient)
                return;

            if(Service != null)
                ((ICommunicationObject)Service).Close();

            if(Factory != null) 
                Factory.Close();

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
            var hash = HashUtils.ComputeFnv1aHash(data);

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
                            Type = "WitCom",
                            Transport = $"{TransportType}",
                            Serializer = $"{SerializerType}",
                            UseEncryption = UseEncryption,
                            UseAuthorization = UseAuthorization,
                            Duration = (end - start),
                            DurationMs = (end - start).TotalMilliseconds,
                            Success = result == hash
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

                    var result = await Service.TwoWaysBenchmark(data);

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
                            Success = data.Is(result)
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
            };

            if(dialog.ShowDialog() != true)
                return;

            using (var writer = new StreamWriter(dialog.FileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                csv.WriteRecords(BenchmarkResults);
            
        }

        private void UpdateStatus()
        {
            IsNamedPipe = TransportType == WCFTransportType.NamedPipe;
            IsTcp = TransportType == WCFTransportType.TCP;
            IsHttp = TransportType == WCFTransportType.HTTP;
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
        public IReadOnlyList<WCFTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public WCFTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<WCFSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public WCFSerializerType? SerializerType { get; set; }

        [Notify]
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; private set; } = null!;
        
        [Notify]
        public string? PipeName { get; set; }

        [Notify]
        public int TcpPort { get; set; }

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
        public bool IsTcp { get; private set; }

        [Notify]
        public bool IsHttp { get; private set; }

        [Notify]
        public int BenchmarkAttempts { get; set; }

        [Notify]
        public long DataSize { get; set; }

        private ChannelFactory? Factory { get; set; }

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
