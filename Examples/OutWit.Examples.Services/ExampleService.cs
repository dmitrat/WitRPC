using System.ComponentModel;
using OutWit.Common.Aspects;
using OutWit.Examples.Contracts;

namespace OutWit.Examples.Services
{
    public class ExampleService : IExampleService
    {
        #region Events

        public event ExampleServiceEventHandler ProcessingStarted = delegate { };

        public event ExampleServiceProgressEventHandler ProgressChanged = delegate { };

        public event ExampleServiceProcessingEventHandler ProcessingCompleted = delegate { };

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        #endregion

        #region Constructors

        public ExampleService()
        {
            IsProcessingStarted = false;
        }

        #endregion

        #region IExampleService

        public bool StartProcessing()
        {
            if(CancellationTokenSource != null)
                return false;

            IsProcessingStarted = true;

            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(Process);

            return true;
        }

        public void StopProcessing()
        {
            CancellationTokenSource?.Cancel(false);
        }

        #endregion

        #region Functions

        public void Process()
        {
            ProcessingStatus status = ProcessingStatus.Success;
            for (int i = 1; i <= 100; i++)
            {
                if (CancellationTokenSource?.IsCancellationRequested == true)
                {
                    status = ProcessingStatus.Interrupted;
                    break;
                }

                ProgressChanged(i);

                Thread.Sleep(50);
            }

            ProcessingCompleted(status);
            CancellationTokenSource = null;
            IsProcessingStarted = false;
        }


        #endregion

        #region Properties
        
        [Notify]
        public bool IsProcessingStarted { get; private set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion

    }
}
