using System;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Input;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Random;
using OutWit.Examples.Discovery.Server.Model;

namespace OutWit.Examples.Discovery.Server
{
    /// <summary>
    /// Interaction logic for AddServicePrompt.xaml
    /// </summary>
    public partial class AddServicePrompt : Window
    {
        public static readonly DependencyProperty ItemsSourceProperty = BindingUtils.Register<AddServicePrompt, IEnumerable<Model.TransportType>>(nameof(ItemsSource));

        public static readonly DependencyProperty SelectedTransportProperty = BindingUtils.Register<AddServicePrompt, Model.TransportType>(nameof(SelectedTransport), OnSelectedTransportChanged);

        public static readonly DependencyProperty ServerNameProperty = BindingUtils.Register<AddServicePrompt, string>(nameof(ServerName), OnServerNameChanged);
        
        public static readonly DependencyProperty ServerDescriptionProperty = BindingUtils.Register<AddServicePrompt, string>(nameof(ServerDescription));

        public static readonly DependencyProperty AddressProperty = BindingUtils.Register<AddServicePrompt, string>(nameof(Address));

        public static readonly DependencyProperty CanSetAddressProperty = BindingUtils.Register<AddServicePrompt, bool>(nameof(CanSetAddress));

        public static readonly DependencyProperty PortProperty = BindingUtils.Register<AddServicePrompt, int>(nameof(Port));

        public static readonly DependencyProperty CanSetPortProperty = BindingUtils.Register<AddServicePrompt, bool>(nameof(CanSetPort));

        public static readonly DependencyProperty CanAddServerProperty = BindingUtils.Register<AddServicePrompt, bool>(nameof(CanAddServer));

        public static readonly DependencyProperty OkCmdProperty = BindingUtils.Register<AddServicePrompt, ICommand>(nameof(OkCmd));

        public static readonly DependencyProperty CancelCmdProperty = BindingUtils.Register<AddServicePrompt, ICommand>(nameof(CancelCmd));

        public static readonly DependencyProperty ResetAddressCmdProperty = BindingUtils.Register<AddServicePrompt, ICommand>(nameof(ResetAddressCmd));

        public static readonly DependencyProperty ResetPortCmdProperty = BindingUtils.Register<AddServicePrompt, ICommand>(nameof(ResetPortCmd));

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
            OkCmd = new DelegateCommand(x => Ok());
            CancelCmd = new DelegateCommand(x => Cancel());
            ResetAddressCmd = new DelegateCommand(x => ResetAddress());
            ResetPortCmd = new DelegateCommand(x => ResetPort());
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

        [Bindable]
        public IEnumerable<Model.TransportType> ItemsSource { get; private set; }

        [Bindable]
        public Model.TransportType SelectedTransport { get; set; }

        [Bindable]
        public string ServerName { get; set; }

        [Bindable]
        public string ServerDescription { get; set; }

        [Bindable]
        public string Address { get; set; }

        [Bindable]
        public int Port { get; set; }

        [Bindable]
        public bool CanAddServer { get; private set; }

        [Bindable]
        public bool CanSetAddress { get; private set; }

        [Bindable]
        public bool CanSetPort { get; private set; }

        #endregion

        #region Commands

        [Bindable]
        public ICommand OkCmd { get; private set; }

        [Bindable]
        public ICommand CancelCmd { get; private set; }

        [Bindable]
        public ICommand ResetAddressCmd { get; private set; }

        [Bindable]
        public ICommand ResetPortCmd { get; private set; }

        #endregion

    }
}
