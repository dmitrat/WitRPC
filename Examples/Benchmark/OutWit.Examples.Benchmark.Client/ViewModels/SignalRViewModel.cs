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
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
using OutWit.Examples.Benchmark.Client.Model;
using OutWit.Examples.Benchmark.Client.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Contracts.Utils;

namespace OutWit.Examples.Benchmark.Client.ViewModels
{
    public class SignalRViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_HTTP_PORT = 8082;

        private const int DEFAULT_BENCHMARK_ATTEMPTS = 50;

        private const long DEFAULT_DATA_SIZE = 10_000_000;


        private const string DEFAULT_HOST = "localhost";
        
        #endregion

        #region Constructors

        public SignalRViewModel(ApplicationViewModel applicationVm) 
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
                SignalRTransportType.WebSockets,
                SignalRTransportType.ServerSentEvents,
                SignalRTransportType.LongPolling,
            ];

            TransportType = SignalRTransportType.WebSockets;

            SerializerTypes =
            [
                SignalRSerializerType.MessagePack,
                SignalRSerializerType.Json,
            ];

            SerializerType = SignalRSerializerType.MessagePack;

            UseEncryption = true;
            UseAuthorization = true;
            AuthorizationToken = DEFAULT_TOKEN;

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

            var builder = new HubConnectionBuilder()
                .WithUrl($"http://{Host}:{HttpPort}/{HttpPath}", options =>
                {
                    switch (TransportType)
                    {
                        case SignalRTransportType.WebSockets:
                            options.Transports = HttpTransportType.WebSockets; 
                            break;
                        case SignalRTransportType.LongPolling:
                            options.Transports = HttpTransportType.LongPolling; 
                            break;
                        case SignalRTransportType.ServerSentEvents:
                            options.Transports = HttpTransportType.ServerSentEvents; 
                            break;
                    }
                });

            switch (SerializerType)
            {
                case SignalRSerializerType.Json:
                    builder.AddJsonProtocol();
                    break;
                case SignalRSerializerType.MessagePack:
                    builder.AddMessagePackProtocol();
                    break;
            }

            try
            {
                Service = builder.Build();

                Service.Closed += OnServiceClosed;
                
                await Service.StartAsync();

                CanConnectClient = false;
                CanDisconnectClient = true;
            }
            catch (Exception e)
            {
                
                
            }
    

          
        }


        private void DisconnectClient()
        {
            if(!CanDisconnectClient)
                return;

            if (Service != null)
            {
                Service.StopAsync().Wait();
                Service.DisposeAsync();
            }

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

                    var result = await Service.InvokeAsync<long>("OneWayBenchmark", data);

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

                    var result = await Service.InvokeAsync<byte[]>("TwoWaysBenchmark", data);

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
            var fileName = $"SignalR_{TransportType}_{SerializerType}_{BenchmarkResults.FirstOrDefault()?.Name}";
            return $"{fileName}.csv";

        }
        
        private void UpdateStatus()
        {
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.IsProperty((WitComViewModel vm)=>vm.TransportType))
                UpdateStatus();
        }


        private async Task OnServiceClosed(Exception? arg)
        {
            Console.WriteLine(arg);
        }

        #endregion

        #region Properties

        [Notify] 
        public IReadOnlyList<SignalRTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public SignalRTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<SignalRSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public SignalRSerializerType? SerializerType { get; set; }

        [Notify]
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; private set; } = null!;

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
        public int BenchmarkAttempts { get; set; }


        [Notify]
        public string Host { get; set; }

        [Notify]
        public long DataSize { get; set; }

        private HubConnection? Service { get; set; }

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
