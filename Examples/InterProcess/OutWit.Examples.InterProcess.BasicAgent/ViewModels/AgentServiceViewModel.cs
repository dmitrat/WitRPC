using System;
using System.Windows;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Communication.Server;
using OutWit.Examples.Contracts;
using OutWit.Examples.InterProcess.BasicAgent.Utils;
using OutWit.Examples.Services;

namespace OutWit.Examples.InterProcess.BasicAgent.ViewModels
{
    internal class AgentServiceViewModel : ViewModelBase<ApplicationViewModel>
    {
        #region Constants

        private const string ACCESS_TOKEN = "token";

        #endregion

        #region Constructors

        public AgentServiceViewModel(ApplicationViewModel applicationVm) 
            : base(applicationVm)
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            ExampleService = new ExampleService();
        }

        #endregion

        public bool Start()
        {
            Server = WitComServerBuilder.Build(options =>
            {
                options.WithService(ExampleService);
                options.WithTransport(ApplicationVm.Parameters.TransportType, ApplicationVm.Parameters.Address);
                options.WithEncryption();
                options.WithJson();
                options.WithAccessToken(ACCESS_TOKEN);
                options.WithTimeout(TimeSpan.FromSeconds(1));
            });
            Server.StartWaitingForConnection();

            return true;
        }

        #region Properties

        private WitComServer? Server { get; set; }

        private IExampleService ExampleService { get; set; } = null!;

        #endregion
    }
}
