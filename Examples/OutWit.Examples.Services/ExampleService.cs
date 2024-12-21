using OutWit.Examples.Contracts;

namespace OutWit.Examples.Services
{
    public class ExampleService : IExampleService
    {
        #region Events

        public event ExampleServiceEventHandler ProcessingStarted = delegate { };

        public event ExampleServiceProgressEventHandler ProgressChanged = delegate { };

        public event ExampleServiceProcessingEventHandler ProcessingCompleted = delegate { };

        #endregion

        #region IExampleService

        public bool StartProcessing()
        {
            if(CancellationTokenSource != null)
                return false;

            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(Process);

            return true;
        }

        public void StopProcessing()
        {
            CancellationTokenSource?.Cancel(false);
        }

        public bool IsProcessingStarted()
        {
            return CancellationTokenSource != null;
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
        }


        #endregion

        #region Properties

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        #endregion
    }
}
