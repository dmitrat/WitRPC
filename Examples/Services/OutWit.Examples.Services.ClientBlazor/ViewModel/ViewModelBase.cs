using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace OutWit.Examples.Services.ClientBlazor.ViewModel
{
    public class ViewModelBase : ComponentBase, INotifyPropertyChanged, IDisposable
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        #endregion

        #region Constructors

        protected ViewModelBase()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        #endregion

        #region Functions

        protected TResult Check<TResult>(Func<TResult> action, TResult onError = default)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return onError;
            }
        }

        protected bool Check(Func<bool> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected void Check(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
            }
        }

        #endregion

        #region EventHandlers

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {

        }

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
        }

        #endregion
    }
}
