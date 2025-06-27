using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;
using OutWit.Communication.Server;
using OutWit.Examples.Contracts;
using OutWit.Examples.Discovery.Server.Model;
using OutWit.Examples.Discovery.Server.Utils;
using OutWit.Examples.Services;

namespace OutWit.Examples.Discovery.Server.ViewModels
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
            ExampleService = new ExampleService();
            Servers = new ObservableCollection<WitServer>();
            SelectedServer = null;
        }

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            AddServerCmd = new DelegateCommand(x => AddServer());
            RemoveServerCmd = new DelegateCommand(x => RemoveServer());
        }

        #endregion

        #region Functions

        private void AddServer()
        {
            var dialog = new AddServicePrompt
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SelectedTransport = TransportType.NamedPipe,
                ServerName = $"Server #{Servers.Count + 1}",
            };

            if (dialog.ShowDialog() != true)
                return;

            var server = WitServerBuilder.Build(options =>
            {
                options.WithService(ExampleService);
                options.WithTransport(dialog.SelectedTransport, dialog.Address, dialog.Port);
                options.WithEncryption();
                options.WithJson();
                options.WithDiscovery();
                options.WithAccessToken(ACCESS_TOKEN);
                options.WithTimeout(TimeSpan.FromSeconds(1));
                options.WithName(dialog.ServerName);
                options.WithDescription(dialog.ServerDescription);
            });
            server.StartWaitingForConnection();

            Servers.Add(server);
            SelectedServer = server;
        }

        private void RemoveServer()
        {
            if(SelectedServer == null)
                return;

            SelectedServer.StopWaitingForConnection();
            Servers.Remove(SelectedServer);

            SelectedServer = null;
        }

        private void UpdateSelectedService()
        {
            HasSelectedServer = SelectedServer != null;

            if (SelectedServer == null)
                return;

            Name = SelectedServer.Name;
            Description = SelectedServer.Description;

            Transport = SelectedServer.Options.Transport;
            Data = string.Join('\n', SelectedServer.Options.Data.Select(x => $"{x.Key}: {x.Value}"));
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((ServicesViewModel vm) => vm.SelectedServer))
                UpdateSelectedService();
        }

        #endregion

        #region Properties

        private IExampleService ExampleService { get; set; } = null!;

        [Notify]
        public ObservableCollection<WitServer> Servers { get; private set; }

        [Notify]
        public WitServer? SelectedServer { get; set; }

        [Notify]
        public string? Transport { get; private set; }

        [Notify]
        public string? Data { get; private set; }

        [Notify]
        public string? Name { get; set; }

        [Notify]
        public string? Description { get; set; }

        [Notify]
        public bool HasSelectedServer { get; private set; }

        #endregion

        #region ICommands

        [Notify]
        public ICommand AddServerCmd { get; private set; }

        [Notify]
        public ICommand RemoveServerCmd { get; private set; }

        #endregion
    }
}
