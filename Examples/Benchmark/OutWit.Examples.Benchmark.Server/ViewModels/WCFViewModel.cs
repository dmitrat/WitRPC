using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Input;
using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Common.Utils;
using OutWit.Examples.Benchmark.Server.Model;
using OutWit.Examples.Benchmark.Server.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services;

namespace OutWit.Examples.Benchmark.Server.ViewModels
{
    public class WCFViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_TCP_PORT = 8081;

        private const int DEFAULT_HTTP_PORT = 8082;

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
            Service = new BenchmarkService();

            TransportTypes =
            [
                WCFTransportType.HTTP,
                WCFTransportType.NamedPipe,
                WCFTransportType.TCP
            ];

            TransportType = WCFTransportType.HTTP;

            SerializerTypes =
            [
                WCFSerializerType.DataContract,
            ];

            SerializerType = WCFSerializerType.DataContract;

            UseEncryption = true;
            UseAuthorization = true;
            AuthorizationToken = DEFAULT_TOKEN;

            PipeName = DEFAULT_PIPE_NAME;
            TcpPort = DEFAULT_TCP_PORT;
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
            ResetPipeNameCmd = new DelegateCommand(x => ResetPipeName());
            ResetHttpPathCmd = new DelegateCommand(x => ResetWebSocketPath());
            ResetHttpPortCmd = new DelegateCommand(x => ResetWebSocketPort());
            ResetTcpPortCmd = new DelegateCommand(x => ResetTcpPort());
        }

        #endregion

        #region Functions

        private void StartServer()
        {
            if(!CanStartServer)
                return;

            Server = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
            {
                if(IsHttp)
                    webBuilder.UseKestrel(options => options.ListenLocalhost(HttpPort));
                else if (IsTcp)
                    webBuilder.UseNetTcp(options => options.Listen($"net.tcp://localhost:{TcpPort}"));
                else if(IsNamedPipe)
                    webBuilder.UseNetNamedPipe(options=>options.Listen($"net.pipe://localhost/{PipeName}"));

                webBuilder.ConfigureServices(services =>
                {
                    services.AddServiceModelServices();
                });

                webBuilder.Configure(app =>
                {
                    app.UseServiceModel(serviceBuilder =>
                    {
                        serviceBuilder.AddService<BenchmarkService>();

                        if(IsHttp)
                            serviceBuilder.AddServiceEndpoint<BenchmarkService, IBenchmarkService>(
                                new BasicHttpBinding
                                {
                                    MaxReceivedMessageSize = 10*1024*1024
                                }, $"/{HttpPath}");


                        if (IsTcp)
                            serviceBuilder.AddServiceEndpoint<BenchmarkService, IBenchmarkService>(
                                new NetTcpBinding
                                {
                                    MaxReceivedMessageSize = 10 * 1024 * 1024
                                }, $"net.tcp://localhost:{TcpPort}");

                        if (IsNamedPipe)
                            serviceBuilder.AddServiceEndpoint<BenchmarkService, IBenchmarkService>(
                                new NetNamedPipeBinding
                                {
                                    MaxReceivedMessageSize = 10 * 1024 * 1024
                                }, $"net.pipe://localhost/{PipeName}");
                    });
                });
            }).Build();


            Server.StartAsync();

            CanStartServer = false;
            CanStopServer = true;
        }

        private async void StopServer()
        {
            if(!CanStopServer)
                return;

            if(Server != null)
                await Server.StopAsync();

            Server = null;

            CanStartServer = true;
            CanStopServer = false;
        }

        private void ResetPipeName()
        {
            PipeName = RandomUtils.RandomString();
        }

        private void ResetWebSocketPath()
        {
            HttpPath = RandomUtils.RandomString();
        }

        private void ResetWebSocketPort()
        {
            HttpPort = NetworkUtils.NextFreePort();
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
            IsNamedPipe = TransportType == WCFTransportType.NamedPipe;
            IsTcp = TransportType == WCFTransportType.TCP;
            IsHttp = TransportType == WCFTransportType.HTTP;
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
        public IReadOnlyList<WCFTransportType> TransportTypes { get; private set; } = null!;

        [Notify]
        public WCFTransportType? TransportType { get; set; }

        [Notify] 
        public IReadOnlyList<WCFSerializerType> SerializerTypes { get; private set; } = null!;

        [Notify]
        public WCFSerializerType? SerializerType { get; set; }

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
        public bool CanStartServer { get; private set; }

        [Notify]
        public bool CanStopServer { get; private set; }

        [Notify]
        public bool IsNamedPipe { get; private set; }

        [Notify]
        public bool IsTcp { get; private set; }

        [Notify]
        public bool IsHttp { get; private set; }

        private IHost? Server { get; set; }

        private IBenchmarkService Service { get; set; } = null!;

        #endregion

        #region Commands

        [Notify] 
        public ICommand StartServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand StopServerCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetPipeNameCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetHttpPathCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetHttpPortCmd { get; private set; } = null!;

        [Notify]
        public ICommand ResetTcpPortCmd { get; private set; } = null!;

        #endregion
    }
}
