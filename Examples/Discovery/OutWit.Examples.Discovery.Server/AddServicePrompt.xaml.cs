using System;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.MVVM.Attributes;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.Random;
using OutWit.Examples.Discovery.Server.Model;

namespace OutWit.Examples.Discovery.Server
{
    /// <summary>
    /// Interaction logic for AddServicePrompt.xaml
    /// </summary>
    public partial class AddServicePrompt : Window
    {
        #region Constructors

        public AddServicePrompt()
        {
            InitializeComponent();
            InitDefaults();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            ItemsSource = Enum.GetValues(typeof(Model.TransportType)).Cast<Model.TransportType>();
        }

        private void InitEvents()
        {
            this.Loaded += OnLoaded;
        }

        private void InitCommands()
        {
            OkCmd = new RelayCommand(x => Ok());
            CancelCmd = new RelayCommand(x => Cancel());
            ResetAddressCmd = new RelayCommand(x => ResetAddress());
            ResetPortCmd = new RelayCommand(x => ResetPort());
        }

        #endregion

        #region Functions

        private void Ok()
        {
            DialogResult = true;
            Close();
        }

        private void Cancel()
        {
            DialogResult = false;
            Close();
        }

        private void ResetAddress()
        {
            Address = RandomUtils.RandomString();
        }

        private void ResetPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            listener.Stop();

            Port = port;
        }

        private void UpdateStatus()
        {
            CanAddServer = !string.IsNullOrEmpty(ServerName);

            switch (SelectedTransport)
            {
                case Model.TransportType.MemoryMappedFile:
                    CanSetAddress = true;
                    CanSetPort = false;
                    break;
                case Model.TransportType.NamedPipe:
                    CanSetAddress = true;
                    CanSetPort = false;
                    break;
                case Model.TransportType.TCP:
                    CanSetAddress = false;
                    CanSetPort = true;
                    break;
                case Model.TransportType.WebSocket:
                    CanSetAddress = true;
                    CanSetPort = true;
                    break;
            }

            CheckParameters();
        }

        private void CheckParameters()
        {
            if (CanSetAddress && string.IsNullOrEmpty(Address))
                ResetAddress();

            if (CanSetPort && Port == 0)
                ResetPort();
        }

        #endregion

        #region Event Handler

        private static void OnServerNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var prompt = d as AddServicePrompt;
            prompt?.UpdateStatus();
        }

        private static void OnSelectedTransportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var prompt = d as AddServicePrompt;
            prompt?.UpdateStatus();
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckParameters();
        }

        #endregion

        #region Properties

        [StyledProperty]
        public IEnumerable<Model.TransportType> ItemsSource { get; private set; }

        [StyledProperty]
        public Model.TransportType SelectedTransport { get; set; }

        [StyledProperty]
        public string ServerName { get; set; }

        [StyledProperty]
        public string ServerDescription { get; set; }

        [StyledProperty]
        public string Address { get; set; }

        [StyledProperty]
        public int Port { get; set; }

        [StyledProperty]
        public bool CanAddServer { get; private set; }

        [StyledProperty]
        public bool CanSetAddress { get; private set; }

        [StyledProperty]
        public bool CanSetPort { get; private set; }

        #endregion

        #region Commands

        [StyledProperty]
        public ICommand OkCmd { get; private set; }

        [StyledProperty]
        public ICommand CancelCmd { get; private set; }

        [StyledProperty]
        public ICommand ResetAddressCmd { get; private set; }

        [StyledProperty]
        public ICommand ResetPortCmd { get; private set; }

        #endregion

    }
}
