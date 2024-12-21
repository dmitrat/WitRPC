using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services;

namespace OutWit.Examples.InterProcess.Basic.Pipes.Agent.ViewModels
{
    internal class AgentServiceViewModel : ViewModelBase<ApplicationViewModel>
    {
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
            PipeName = RandomUtils.RandomString();
            Server = WitComServerBuilder.Build(options =>
            {
                options.WithService(ExampleService);
                options.WithNamedPipe(PipeName);
                options.WithEncryption();
                options.WithJson();
                options.WithAccessToken("token");
            });
            Server.StartWaitingForConnection();

            return true;
        }

        #region Properties

        private string? PipeName { get; set; }

        private WitComServer? Server { get; set; }

        private IExampleService ExampleService { get; set; } = null!;

        #endregion
    }
}
