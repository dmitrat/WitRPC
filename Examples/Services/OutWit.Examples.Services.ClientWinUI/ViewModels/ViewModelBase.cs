using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Services.ClientWinUI.ViewModels
{
    public abstract class ViewModelBase<TApplicationVm> : INotifyPropertyChanged, IDisposable
      where TApplicationVm : class
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        protected ViewModelBase(TApplicationVm applicationVm)
        {
            ApplicationVm = applicationVm;
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

        protected async Task CheckAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
            }
        }

        protected async Task<TResult> CheckAsync<TResult>(Func<Task<TResult>> action, TResult onError = default)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                return onError;
            }
        }

        protected async Task<IReadOnlyCollection<TResult>> CheckAsync<TResult>(Func<Task<IReadOnlyCollection<TResult>>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                return Array.Empty<TResult>();
            }
        }

        protected async Task<IReadOnlyList<TResult>> CheckAsync<TResult>(Func<Task<IReadOnlyList<TResult>>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                return Array.Empty<TResult>();
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

        #region IDisposable

        public virtual void Dispose()
        {
        }

        #endregion

        #region Properties

        protected TApplicationVm ApplicationVm { get; }

        #endregion
    }
}
