using System;
using System.ComponentModel;
using System.Windows.Input;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Communication.Client;
using OutWit.Communication.Messages;
using OutWit.Examples.Contracts;

namespace OutWit.Examples.Discovery.Client.Model
{
    public class ClientInfo : NotifyPropertyChangedBase, IDisposable
    {
        #region Constructors

        public ClientInfo(DiscoveryMessage message, WitComClient client)
        {
            ServiceId = message.ServiceId!.Value;
            ServiceName = message.ServiceName;
            Transport = message.Transport;
            Client = client;

            Service = client.GetService<IExampleService>();

            InitDefaults();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Progress = 0;
            CanStartProcess = true;
            CanInterruptProcess = false;
        }

        private void InitEvents()
        {
            Service.ProgressChanged += OnProgressChanged;
            Service.ProcessingStarted += OnProcessingStarted;
            Service.ProcessingCompleted += OnProcessingCompleted;
        }

        private void InitCommands()
        {
            StartProcessingCmd = new DelegateCommand(x => StartProcessing());
            InterruptProcessingCmd = new DelegateCommand(x => InterruptProcessing());
        }

        #endregion

        #region Functions

        public void StartProcessing()
        {
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
            try
            {
                Service.StopProcessing();
            }
            catch (Exception e)
            {
            }
        }

        #endregion

        #region Event Handlers

        private void OnProcessingCompleted(ProcessingStatus status)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CanStartProcess = true;
                CanInterruptProcess = false;

                Progress = 0;
                ProcessingStatus = status;
            });
        }

        private void OnProcessingStarted()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                CanStartProcess = false;
                CanInterruptProcess = true;
            });
        }

        private void OnProgressChanged(double progress)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Progress = progress;
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                Service.StopProcessing();
                Client.Disconnect().Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception e)
            {



            }
        }

        #endregion

        #region Properties

        public Guid ServiceId { get; }

        public string? ServiceName { get; }

        public string? Transport { get;  }

        public WitComClient? Client { get; }

        public IExampleService Service { get; }

        [Notify]
        public double Progress { get; private set; }

        [Notify]
        public bool CanStartProcess { get; private set; }

        [Notify]
        public bool CanInterruptProcess { get; private set; }

        [Notify]
        public ProcessingStatus ProcessingStatus { get; private set; }

        public ICommand StartProcessingCmd { get; private set; }

        public ICommand InterruptProcessingCmd { get; private set; }

        #endregion
    }
}
