using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OutWit.Examples.Services.ClientWinUI.Commands
{
    public class DelegateCommand : ICommand
    {
        #region Events

        public event EventHandler CanExecuteChanged = delegate { };

        #endregion

        #region Fields

        private readonly Predicate<object> m_canExecute;

        private readonly Action<object> m_execute;

        #endregion

        #region Constructors

        public DelegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }


        public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            m_execute = execute;
            m_canExecute = canExecute;
        }

        #endregion

        #region Functions

        public bool CanExecute(object parameter)
        {
            return m_canExecute == null || m_canExecute(parameter);
        }


        public void Execute(object parameter)
        {
            m_execute(parameter);
        }

        #endregion


    }
}
