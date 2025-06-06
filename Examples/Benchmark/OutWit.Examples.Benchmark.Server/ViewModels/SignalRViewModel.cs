using System;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using OutWit.Common.Aspects;
using System.Windows.Input;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Examples.Benchmark.Server.Model;
using OutWit.Examples.Benchmark.Server.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services;
using Microsoft.AspNetCore.SignalR;
using OutWit.Common.Utils;

namespace OutWit.Examples.Benchmark.Server.ViewModels
{   
    public class SignalRViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Classes

        public class BenchmarkHub : Hub, IBenchmarkService
        {
            public BenchmarkHub()
            {
                Service = new BenchmarkService();
            }

            public async Task<long> OneWayBenchmark(byte[] bytes)
            {
                return await Service.OneWayBenchmark(bytes);
            }

            public async Task<byte[]> TwoWaysBenchmark(byte[] bytes)
            {
                return await Service.TwoWaysBenchmark(bytes);
            }

            private IBenchmarkService Service { get; }


        }

        #endregion

        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_HTTP_PORT = 8082;

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
            ResetHttpPathCmd = new DelegateCommand(x => ResetWebSocketPath());
            ResetHttpPortCmd = new DelegateCommand(x => ResetWebSocketPort());
        }

        #endregion

        #region Functions

        private async void StartServer()
        {
            if (!CanStartServer)
                return;

            Server = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        
                        options.ListenLocalhost(HttpPort);
                        options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
                    });

                    webBuilder.ConfigureServices(services =>
                    {
                        var hubBuilder = services.AddSignalR(options =>
                        {
                            options.MaximumReceiveMessageSize = 64 * 1024 * 1024;
                        });
                        if (SerializerType == SignalRSerializerType.MessagePack)
                            hubBuilder.AddMessagePackProtocol();
                        if (SerializerType == SignalRSerializerType.Json)
                            hubBuilder.AddJsonProtocol();
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseWebSockets();
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<BenchmarkHub>($"/{HttpPath ?? ""}", options =>
                            {
                                if (TransportType == SignalRTransportType.WebSockets)
                                    options.Transports = HttpTransportType.WebSockets;

                                if (TransportType == SignalRTransportType.ServerSentEvents)
                                    options.Transports = HttpTransportType.ServerSentEvents;

                                if (TransportType == SignalRTransportType.LongPolling)
                                    options.Transports = HttpTransportType.LongPolling;
                            });
                        });
                    });
                }).Build();


            await Server.StartAsync();

            CanStartServer = false;
            CanStopServer = true;
        }

        private async void StopServer()
        {
            if (!CanStopServer)
                return;

            if (Server != null)
                await Server.StopAsync();

            Server = null;

            CanStartServer = true;
            CanStopServer = false;
        }

        private void ResetWebSocketPath()
        {
            HttpPath = RandomUtils.RandomString();
        }

        private void ResetWebSocketPort()
        {
            HttpPort = NetworkUtils.NextFreePort();
        }

        private void ResetAuthorizationToken()
        {
            AuthorizationToken = RandomUtils.RandomString();
        }

        private void UpdateStatus()
        {
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((WitComViewModel vm) => vm.TransportType))
                UpdateStatus();
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
        public bool CanStartServer { get; private set; }

        [Notify]
        public bool CanStopServer { get; private set; }

        private IHost? Server { get; set; }

        #endregion

        #region Commands

        [Notify]
        public ICommand StartServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand StopServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetHttpPathCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetHttpPortCmd { get; private set; } = null!;

        #endregion
    }
}
