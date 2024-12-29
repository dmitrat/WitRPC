using System;
using System.Windows;
using OutWit.Examples.InterProcess.Shared;
using OutWit.InterProcess.Agent;
using OutWit.InterProcess.Model;

namespace OutWit.Examples.InterProcess.BasicAgent.ViewModels
{
    internal class ApplicationViewModel
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
            AgentServiceVm = new AgentServiceViewModel(this);
        }

        #endregion

        #region Functions

        public void Run(StartupParametersTransport parameters)
        {
            if (string.IsNullOrEmpty(parameters.Address))
                return;

            Parameters = parameters;

            Application = new AgentApplication(parameters);
            Application.Startup += OnStartup;
            Application.Run();
        }

        #endregion

        #region Event Handlers

        private void OnStartup(object sender, StartupEventArgs e)
        {
            if (!AgentServiceVm.Start())
                Application.Shutdown(0);
        }

        #endregion

        #region Properties

        public AgentServiceViewModel AgentServiceVm { get; private set; }

        public AgentApplication Application { get; private set; }

        public StartupParametersTransport Parameters { get; private set; }

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
