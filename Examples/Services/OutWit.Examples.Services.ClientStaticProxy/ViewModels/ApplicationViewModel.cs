using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Services.ClientStaticProxy.ViewModels
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
            ServiceVm = new ServiceViewModel(this);
        }

        #endregion

        #region Properties

        public ServiceViewModel ServiceVm { get; private set; }

        #endregion
    }
}
