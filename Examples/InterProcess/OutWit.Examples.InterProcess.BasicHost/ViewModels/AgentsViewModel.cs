using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.Extensions.Logging;
using OutWit.Common.Aspects;
using OutWit.Common.Logging;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;
using OutWit.Communication.Client;
using OutWit.Examples.InterProcess.BasicHost.Models;
using OutWit.Examples.InterProcess.BasicHost.Utils;
using OutWit.Examples.InterProcess.Shared;
using OutWit.InterProcess.Host.Utils;

namespace OutWit.Examples.InterProcess.BasicHost.ViewModels
{
    public class AgentsViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string AGENT_FOLDER = "@Agent";

        private const string ACCESS_TOKEN = "token";

        #endregion

        #region Constructors

        public AgentsViewModel(ApplicationViewModel applicationVm) :
            base(applicationVm)
        {
            InitDefaults();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Agents = new ObservableCollection<AgentInfo>();
            Logger = new SimpleLogger("agent", LogLevel.Debug);
        }

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            AddAgentCmd = new RelayCommand(x => AddAgent());
            RemoveAgentCmd = new RelayCommand(x => RemoveAgent());
            StartProcessingCmd = new RelayCommand(x => StartProcessing());
            InterruptProcessingCmd = new RelayCommand(x => InterruptProcessing());
        }

        #endregion

        #region Functions

        private async void AddAgent()
        {
            var dialog = new TransportPromptDialog
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SelectedTransport = TransportType.Pipes
            };

            if(dialog.ShowDialog() != true)
                return;

            var agentFolder = Path.GetFullPath(AGENT_FOLDER);
            if(!Directory.Exists(agentFolder))
                return;

            var agentPath = Directory.EnumerateFiles(agentFolder, "*.exe").FirstOrDefault();
            if(string.IsNullOrEmpty(agentPath) || !File.Exists(agentPath))
                return;

            var parameters = dialog.SelectedTransport.GetParameters(Logger);

            var process = HostUtils.RunAgent(agentPath, parameters);
            if(process == null)
                return;

            Thread.Sleep(500);

            var client = WitClientBuilder.Build(options =>
            {
                options.WithTransport(dialog.SelectedTransport, parameters.Address);
                options.WithEncryption();
                options.WithJson();
                options.WithAccessToken(ACCESS_TOKEN);
                options.WithLogger(Logger);
                options.WithTimeout(TimeSpan.FromSeconds(1));
            });

            var result = await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            if (!result)
            {
                process.Kill();
                return;
            }

            var agent = new AgentInfo(process, client, dialog.SelectedTransport);
            Agents?.Add(agent);

            SelectedAgent = agent;
        }

        private void RemoveAgent()
        {
            if(SelectedAgent == null)
                return;

            try
            {
                SelectedAgent.Dispose();
                Agents?.Remove(SelectedAgent);
            }
            catch (Exception e)
            {
                

            }

            
        }

        private void StartProcessing()
        {
            if (SelectedAgent == null)
                return;


            if(SelectedAgent.CanStartProcess)
                SelectedAgent.StartProcessing();
        }

        private void InterruptProcessing()
        {
            if (SelectedAgent == null)
                return;

            if (SelectedAgent.CanInterruptProcess)
                SelectedAgent.InterruptProcessing();
        }



        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((AgentsViewModel vm) => vm.SelectedAgent))
                HasSelectedAgent = SelectedAgent != null;
        }

        #endregion

        #region Properties

        [Notify]
        public ObservableCollection<AgentInfo>? Agents { get; private set; }

        [Notify]
        public AgentInfo? SelectedAgent { get; set; }

        [Notify]
        public SimpleLogger Logger { get; set; }

        [Notify]
        public bool HasSelectedAgent { get; private set; }

        #endregion

        #region Commands

        [Notify]
        public ICommand? AddAgentCmd { get; private set; }

        [Notify]
        public ICommand? RemoveAgentCmd { get; private set; }

        [Notify]
        public ICommand? StartProcessingCmd { get; private set; }

        [Notify]
        public ICommand? InterruptProcessingCmd { get; private set; }

        #endregion
    }
}
