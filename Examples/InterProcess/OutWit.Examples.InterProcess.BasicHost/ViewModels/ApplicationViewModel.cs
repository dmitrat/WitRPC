using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.InterProcess.BasicHost.ViewModels
{
    public class ApplicationViewModel
    {
        #region Constructors

        public ApplicationViewModel()
        {
            InitViewModels();
        }

        #endregion

        #region Initialization

        private void InitViewModels()
        {
            AgentsVm = new AgentsViewModel(this);
        }

        #endregion

        #region Properties

        public AgentsViewModel AgentsVm { get; private set; }

        #endregion
    }
}
