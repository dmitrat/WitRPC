using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using OutWit.Common.Aspects;
using OutWit.Common.Logging;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Communication.Interfaces;
using OutWit.Examples.Contracts;
using static OutWit.Common.MVVM.Utils.Extensions;

namespace OutWit.Examples.Services.ClientPipes.ViewModels
{
    public class ServiceViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        internal const string PIPE_NAME = "servicepipe";

        #endregion

        #region Constructors

        public ServiceViewModel(ApplicationViewModel applicationVm) 
            : base(applicationVm)
        {
            InitDefaults();
            InitCommands();

            Reconnect();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Logger = new SimpleLogger("Pipes Client");
        }

        private void InitCommands()
        {
            ReconnectCmd = new DelegateCommand(x=> Reconnect());
            StartProcessingCmd = new DelegateCommand(x=> StartProcessing());
            InterruptProcessingCmd = new DelegateCommand(x=>InterruptProcessing());
            InterruptProcessingCmd = new DelegateCommand(x => InterruptProcessing());
            InterruptProcessingAsyncCmd = new DelegateCommand(x => InterruptProcessingAsync());
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

                Client = WitClientBuilder.Build(options =>
                {
                    options.WithNamedPipe(PIPE_NAME);
                    options.WithEncryption();
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
                    Service = Client.GetService<IExampleService>();

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

        private void StartProcessing()
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

        private async void StartProcessingAsync()
        {
            if (Service == null || !CanStartProcessing)
                return;

            try
            {
                await Service.StartProcessingAsync();
            }
            catch (Exception e)
            {


            }
        }

        private void InterruptProcessing()
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

        private async void InterruptProcessingAsync()
        {
            if (Service == null || !CanInterruptProcessing)
                return;
            try
            {
                await Service.StopProcessingAsync();
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
        }

        #endregion

        #region Event Handlers

        private void OnProcessingCompleted(ProcessingStatus status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CanStartProcessing = true;
                CanInterruptProcessing = false;

                Progress = 0;
                ProcessingStatus = status;
            });
        }

        private void OnProcessingStarted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CanStartProcessing = false;
                CanInterruptProcessing = true;
            });
        }

        private void OnProgressChanged(double progress)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Progress = progress;
            });
        }

        private void OnClientDisconnected(Guid sender)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Thread.Sleep(500);
                Logger?.LogInformation("Client disconnected, trying to reconnect");
                Reconnect();
            });
            
        }

        #endregion

        #region Properties

        private WitClient? Client { get; set; }

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

        #region Commands

        [Notify]
        public ICommand? ReconnectCmd { get; private set; }

        [Notify]
        public ICommand? StartProcessingCmd { get; private set; }

        [Notify]
        public ICommand? InterruptProcessingCmd { get; private set; }

        [Notify]
        public ICommand? StartProcessingAsyncCmd { get; private set; }

        [Notify]
        public ICommand? InterruptProcessingAsyncCmd { get; private set; }

        #endregion
    }
}
