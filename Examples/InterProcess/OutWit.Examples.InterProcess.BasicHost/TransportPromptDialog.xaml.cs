using OutWit.Common.MVVM.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Examples.InterProcess.Shared;

namespace OutWit.Examples.InterProcess.BasicHost
{
    /// <summary>
    /// Interaction logic for TransportPromptDialog.xaml
    /// </summary>
    public partial class TransportPromptDialog : Window
    {
        public static readonly DependencyProperty ItemsSourceProperty = BindingUtils.Register<TransportPromptDialog, IEnumerable<TransportType>>(nameof(ItemsSource));

        public static readonly DependencyProperty SelectedTransportProperty = BindingUtils.Register<TransportPromptDialog, TransportType>(nameof(SelectedTransport));

        public static readonly DependencyProperty OkCmdProperty = BindingUtils.Register<TransportPromptDialog, ICommand>(nameof(OkCmd));

        public static readonly DependencyProperty CancelCmdProperty = BindingUtils.Register<TransportPromptDialog, ICommand>(nameof(CancelCmd));

        #region Constructors

        public TransportPromptDialog()
        {
            InitializeComponent();
            InitDefaults();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            ItemsSource = Enum.GetValues(typeof(TransportType)).Cast<TransportType>();
        }

        private void InitCommands()
        {
            OkCmd = new DelegateCommand(x => Ok());
            CancelCmd = new DelegateCommand(x => Cancel());
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

        #endregion

        #region Properties

        [Bindable]
        public IEnumerable<TransportType> ItemsSource { get; private set; }

        [Bindable]
        public TransportType SelectedTransport { get; set; }

        [Bindable]
        public ICommand OkCmd { get; private set; }

        [Bindable]
        public ICommand CancelCmd { get; private set; }

        #endregion
    }
}
