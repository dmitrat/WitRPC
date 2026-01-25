
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
using OutWit.Common.MVVM.Attributes;
using OutWit.Common.MVVM.Commands;
using OutWit.Examples.InterProcess.Shared;

namespace OutWit.Examples.InterProcess.BasicHost
{
    /// <summary>
    /// Interaction logic for TransportPromptDialog.xaml
    /// </summary>
    public partial class TransportPromptDialog : Window
    {
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
            OkCmd = new RelayCommand(x => Ok());
            CancelCmd = new RelayCommand(x => Cancel());
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

        [StyledProperty]
        public IEnumerable<TransportType> ItemsSource { get; private set; }

        [StyledProperty]
        public TransportType SelectedTransport { get; set; }

        [StyledProperty]
        public ICommand OkCmd { get; private set; }

        [StyledProperty]
        public ICommand CancelCmd { get; private set; }

        #endregion
    }
}
