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
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OutWit.Common.Serialization;
using OutWit.Common.Utils;
using ProtoBuf.Grpc.Server;

namespace OutWit.Examples.Benchmark.Server.ViewModels
{   
    public class GRPCViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string DEFAULT_TOKEN = "Token";

        private const string DEFAULT_PIPE_NAME = "np";

        private const string DEFAULT_HTTP_PATH = "api";

        private const int DEFAULT_HTTP_PORT = 8082;

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
            Service = new BenchmarkService();

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
        }

        #endregion

        #region Functions

        private async void StartServer()
        {
            if (!CanStartServer)
                return;

            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddCodeFirstGrpc(options =>
                {
                    options.MaxReceiveMessageSize = 64 * 1024 * 1024;
                    options.MaxSendMessageSize = 64 * 1024 * 1024;
                });
                builder.WebHost.ConfigureKestrel(opts =>
                {
                    if (IsHttp)
                        opts.ListenLocalhost(HttpPort, lo => lo.Protocols = HttpProtocols.Http2);
                    else if (IsNamedPipe)
                        opts.ListenNamedPipe(PipeName!, lo => lo.Protocols = HttpProtocols.Http2);
                });

                Server = builder.Build();
                Server.MapGrpcService<BenchmarkService>();

                await Server.StartAsync();

                CanStartServer = false;
                CanStopServer = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

          
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

        private void ResetAuthorizationToken()
        {
            AuthorizationToken = RandomUtils.RandomString();
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
            if (e.IsProperty((WitRPCViewModel vm) => vm.TransportType))
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
        public bool CanStartServer { get; private set; }

        [Notify]
        public bool CanStopServer { get; private set; }

        [Notify]
        public bool IsNamedPipe { get; private set; }

        [Notify]
        public bool IsHttp { get; private set; }

        private WebApplication? Server { get; set; }

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

        #endregion
    }
}
