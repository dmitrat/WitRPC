using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Services.ClientWinUI.ViewModels
{
    public class ApplicationViewModel
    {
        #region Constructors

        public ApplicationViewModel(App application)
        {
            Application = application;

            InitViewModels();
        }

        #endregion

        #region Functions


        public void Invoke(Action action)
        {
            Application?.Window?.DispatcherQueue.TryEnqueue(() => action());
        }

        #endregion

        #region Initialization

        private void InitViewModels()
        {
            ServiceVm = new ServiceViewModel(this);
        }

        #endregion

        #region Properties

        public App Application { get; }

        public ServiceViewModel ServiceVm { get; private set; }

        #endregion
    }
}
