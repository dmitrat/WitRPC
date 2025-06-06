using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using OutWit.Common.Aspects;
using OutWit.Common.Logging;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;
using OutWit.Communication.Client;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Messages;
using OutWit.Examples.Discovery.Client.Model;
using OutWit.Examples.Discovery.Client.Utils;

namespace OutWit.Examples.Discovery.Client.ViewModels
{
    internal class ServicesViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string ACCESS_TOKEN = "token";

        #endregion

        #region Constructors

        public ServicesViewModel(ApplicationViewModel applicationVm) 
            : base(applicationVm)
        {
            InitDefaults();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            DiscoveryClient = WitComClientBuilder.Discovery(options =>
            {

            });

            DiscoveryClient.Start();

            Logger = new SimpleLogger("agent", LogLevel.Debug);

            Clients = new ObservableCollection<ClientInfo>();
            SelectedClient = null;

            Messages = new ObservableCollection<DiscoveryMessage>();
            SelectedMessage = null;
        }

        private void InitEvents()
        {
            DiscoveryClient.MessageReceived += OnMessageReceived;
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            ConnectCmd = new DelegateCommand(x => Connect());
            DisconnectCmd = new DelegateCommand(x => RemoveServer());
        }

        #endregion

        #region Functions

        private async void Connect()
        {
            if (SelectedMessage == null || SelectedMessage.ServiceId == null)
                return;

            var client = WitComClientBuilder.Build(options =>
            {
                options.WithTransport(SelectedMessage);
                options.WithEncryption();
                options.WithJson();
                options.WithAccessToken(ACCESS_TOKEN);
                options.WithLogger(Logger);
                options.WithTimeout(TimeSpan.FromSeconds(1));
            });

            var result = await client.ConnectAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            if (!result)
                return;

            var clientInfo = new ClientInfo(SelectedMessage, client);

            Clients.Add(clientInfo);
            SelectedClient = clientInfo;

            UpdateSelectedMessage();
        }

        private void RemoveServer()
        {
            if(SelectedClient == null)
                return;

            SelectedClient.Dispose();

            Clients.Remove(SelectedClient);

            UpdateSelectedMessage();
        }

        private void UpdateSelectedClient()
        {
            HasSelectedClient = SelectedClient != null;

        }

        private void UpdateSelectedMessage()
        {
            if(SelectedMessage == null)
                CanConnectServer = false;
            else if (SelectedMessage.Type != DiscoveryMessageType.Goodbye)
                CanConnectServer = Clients.All(info => info.ServiceId != SelectedMessage.ServiceId);
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((ServicesViewModel vm) => vm.SelectedClient))
                UpdateSelectedClient();

            if (e.IsProperty((ServicesViewModel vm) => vm.SelectedMessage))
                UpdateSelectedMessage();
        }

        private void OnMessageReceived(IDiscoveryClient sender, DiscoveryMessage message)
        {
            Messages.Add(message);
        }
        #endregion

        #region Properties

        private IDiscoveryClient DiscoveryClient { get; set; } = null!;

        [Notify]
        public ObservableCollection<ClientInfo> Clients { get; private set; }

        [Notify]
        public ObservableCollection<DiscoveryMessage> Messages { get; private set; }

        [Notify]
        public ClientInfo? SelectedClient { get; set; }

        [Notify]
        public DiscoveryMessage? SelectedMessage { get; set; }

        [Notify]
        public SimpleLogger Logger { get; set; }

        [Notify]
        public bool HasSelectedClient { get; private set; }

        [Notify]
        public bool CanConnectServer {get; private set; }

        #endregion

        #region ICommands

        [Notify]
        public ICommand ConnectCmd { get; private set; }

        [Notify]
        public ICommand DisconnectCmd { get; private set; }

        #endregion
    }
}
