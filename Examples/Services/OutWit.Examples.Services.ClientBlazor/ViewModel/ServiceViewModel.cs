using System;
using System.Net.Mime;
using System.Windows.Input;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Common.Aspects;
using OutWit.Common.Logging;
using OutWit.Communication.Client;
using OutWit.Communication.Client.WebSocket.Utils;
using OutWit.Communication.Interfaces;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services.ClientBlazor.Commands;
using OutWit.Examples.Services.ClientBlazor.Encryption;

namespace OutWit.Examples.Services.ClientBlazor.ViewModel
{
    public class ServiceViewModel : ViewModelBase
    {
        #region Constants

        private const int PORT = 5052;

        #endregion

        #region Constructors

        public ServiceViewModel()
        {
            InitDefaults();

        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Logger = new SimpleLogger("Blazor Client");
            Logger.CollectionChanged += (_, __) => StateHasChanged();
        }

        private void InitClient()
        {
            if(Client == null)
                return;

            Client.Disconnected += OnClientDisconnected;
        }

        private void InitService()
        {
            if(Service == null)
                return;

            Service.ProgressChanged += OnProgressChanged;
            Service.ProcessingStarted += OnProcessingStarted;
            Service.ProcessingCompleted += OnProcessingCompleted;
        }

        #endregion

        #region Functions
        
        public async void Reconnect()
        {
            try
            {
                CanReconnect = false;
                Progress = 0;

                Client = WitComClientBuilder.Build(options =>
                {
                    options.WithWebSocket($"ws://localhost:{PORT}/webSocket/");
                    options.WithEncryptor(EncryptorClientWeb);
                    options.WithJson();
                    options.WithLogger(Logger!);
                    options.WithTimeout(TimeSpan.FromSeconds(1));
                });

                var result = await Client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                if (!result)
                {
                    Logger?.LogError("Failed to connect to server");
                    Service = null;
                    Client?.Disconnect().Wait(TimeSpan.FromSeconds(1));
                    Client = null;
                }
                else
                {
                    Service = Client.GetService<IExampleService>(false);
                    //Service = Client.GetService<IExampleService>(x=>new ExampleServiceProxy(x));
                }

            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to connect to server");
                Client = null;
                Service = null;
            }

            UpdateStatus();
            InitClient();
            InitService();
        }

        public void StartProcessing()
        {
            if (Service == null || !CanStartProcessing)
                return;

            try
            {
                Service.StartProcessing();
            }
            catch (Exception e)
            {
                

            }
        }

        public void InterruptProcessing()
        {
            if (Service == null || !CanInterruptProcessing)
                return;
            try
            {
                Service.StopProcessing();
            }
            catch (Exception e)
            {

            }
        }

        private void UpdateStatus()
        {
            CanReconnect = (Client == null || Service == null);
            IsConnected = !CanReconnect;
            CanStartProcessing = !CanReconnect;
            CanInterruptProcessing = false;

            StateHasChanged();
        }

        #endregion

        #region Event Handlers

        protected override async Task OnInitializedAsync()
        {
            await EncryptorClientWeb.InitAsync();

            await base.OnInitializedAsync();

            Reconnect();
        }

        private void OnProcessingCompleted(ProcessingStatus status)
        {
            CanStartProcessing = true;
            CanInterruptProcessing = false;

            Progress = 0;
            ProcessingStatus = status;

            StateHasChanged();
        }

        private void OnProcessingStarted()
        {
            CanStartProcessing = false;
            CanInterruptProcessing = true;

            StateHasChanged();
        }

        private void OnProgressChanged(double progress)
        {
            Progress = progress;
            StateHasChanged();
        }

        private void OnClientDisconnected(Guid sender)
        {
            Thread.Sleep(500);
            Logger?.LogInformation("Client disconnected, trying to reconnect");
            Reconnect();
        }

        #endregion

        #region Properties

        private WitComClient? Client { get; set; }

        private IExampleService? Service { get; set; }

        [Notify]
        public SimpleLogger? Logger { get; private set; }

        [Notify]
        public bool CanReconnect { get; private set; }

        [Notify]
        public bool IsConnected { get; private set; }

        [Notify]
        public bool CanStartProcessing { get; private set; }

        [Notify]
        public bool CanInterruptProcessing { get; private set; }

        [Notify]
        public double Progress { get; private set; }

        [Notify]
        public ProcessingStatus ProcessingStatus { get; private set; }

        #endregion

        #region Injections

        [Inject]
        public EncryptorClientWeb EncryptorClientWeb { get; private set; }

        #endregion

    }
}
