using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.CommandLine;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.Examples.InterProcess.BasicHost.Models;
using OutWit.Examples.InterProcess.BasicHost.Utils;
using OutWit.Examples.InterProcess.Shared;
using OutWit.Examples.InterProcess.Shared.Utils;
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
        }

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            AddAgentCmd = new DelegateCommand(x => AddAgent());
            RemoveAgentCmd = new DelegateCommand(x => RemoveAgent());
            StartProcessingCmd = new DelegateCommand(x => StartProcessing());
            InterruptProcessingCmd = new DelegateCommand(x => InterruptProcessing());
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

            var parameters = dialog.SelectedTransport.GetParameters();

            var process = HostUtils.RunAgent(agentPath, parameters);
            if(process == null)
                return;

            Thread.Sleep(500);

            var client = WitComClientBuilder.Build(options =>
            {
                options.WithTransport(dialog.SelectedTransport, parameters.Address);
                options.WithEncryption();
                options.WithJson();
                options.WithAccessToken(ACCESS_TOKEN);
            });

            var result = await client.ConnectAsync(TimeSpan.Zero, CancellationToken.None);
            if(!result)
                return;

            var agent = new AgentInfo(process, client, dialog.SelectedTransport);
            Agents?.Add(agent);

            SelectedAgent = agent;
        }

        private void RemoveAgent()
        {
            if(SelectedAgent == null)
                return;

            SelectedAgent.Dispose();
            Agents?.Remove(SelectedAgent);
        }

        private void StartProcessing()
        {
            if (SelectedAgent == null)
                return;

            SelectedAgent.Service.StartProcessing();
        }

        private void InterruptProcessing()
        {
            if (SelectedAgent == null)
                return;

            SelectedAgent.Service.StopProcessing();
        }



        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((AgentsViewModel vm) => vm.Agents))
            {
                int hh = 0;
            }
        }

        #endregion

        #region Properties

        [Notify]
        public ObservableCollection<AgentInfo>? Agents { get; private set; }

        [Notify]
        public AgentInfo? SelectedAgent { get; set; }

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
