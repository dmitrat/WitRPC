using System;
using System.Windows;

namespace OutWit.Examples.Benchmark.Client.ViewModels
{
    public class ApplicationViewModel
    {
        #region Static Fields

        private static ApplicationViewModel m_instance;

        #endregion

        #region Constructors

        private ApplicationViewModel()
        {
            InitApplication();
            InitServices();
            InitViewModels();
        }

        #endregion

        #region Initialization

        private void InitApplication()
        {

        }

        private void InitServices()
        {
        }

        private void InitViewModels()
        {
            WitComVm = new WitComViewModel(this);
            WCFVm = new WCFViewModel(this);
            SignalRVm = new SignalRViewModel(this);
            GRPCVm = new GRPCViewModel(this);
        }

        #endregion

        #region Properties

        public WitComViewModel WitComVm { get; private set; }

        public WCFViewModel WCFVm { get; private set; }
        
        public SignalRViewModel SignalRVm { get; set; }
        
        public GRPCViewModel GRPCVm { get; set; }

        #endregion

        #region Static Properties

        public static ApplicationViewModel Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ApplicationViewModel();
                }
                return m_instance;
            }
        }

        #endregion
    }
}
