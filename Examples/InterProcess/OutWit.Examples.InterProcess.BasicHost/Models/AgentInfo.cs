using System;
using System.ComponentModel;
using System.Diagnostics;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Communication.Client;
using OutWit.Examples.Contracts;
using OutWit.Examples.InterProcess.Shared;

namespace OutWit.Examples.InterProcess.BasicHost.Models
{
    public class AgentInfo : INotifyPropertyChanged, IDisposable
    {
        #region Static

        private static int m_count = 0;

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        #endregion

        #region Constructors

        public AgentInfo(Process process, WitComClient client, TransportType transport)
        {
            AgentId = ++m_count;
            Process = process;
            Client = client;
            Service = client.GetService<IExampleService>();
            Transport = transport;

            InitDefaults();
            InitEvents();
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

        #endregion

        #region Functions

        public override string ToString()
        {
            return AgentId.ToString();
        }

        public void StartProcessing()
        {
            try
            {
                Service.StartProcessing();
                IsProcessingStarted = true;
            }
            catch (Exception e)
            {


                int hh = 0;
            }
        }

        public void InterruptProcessing()
        {
            try
            {
                Service.StopProcessing();
                IsProcessingStarted = false;
            }
            catch (Exception e)
            {

                int hh = 0;
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
                IsProcessingStarted = false;
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
           
            Process.Kill();
        }

        #endregion

        #region Properties

        public int AgentId { get; }

        public WitComClient Client { get; }

        public Process Process { get; }

        public IExampleService Service { get; }

        public TransportType Transport { get; }

        [Notify]
        public bool IsProcessingStarted { get; private set; }

        [Notify]
        public double Progress { get; private set; }

        [Notify]
        public bool CanStartProcess { get; private set; }

        [Notify]
        public bool CanInterruptProcess { get; private set; }

        [Notify]
        public ProcessingStatus ProcessingStatus { get; private set; }

        #endregion
    }
}
