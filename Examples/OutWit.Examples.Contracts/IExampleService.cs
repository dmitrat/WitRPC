using System.ComponentModel;

namespace OutWit.Examples.Contracts
{
    public interface IExampleService : INotifyPropertyChanged
    {
        public event ExampleServiceEventHandler ProcessingStarted;
        public event ExampleServiceProgressEventHandler ProgressChanged;
        public event ExampleServiceProcessingEventHandler ProcessingCompleted;

        public bool StartProcessing();
        public void StopProcessing();

        public bool IsProcessingStarted { get; }
    }

    public delegate void ExampleServiceEventHandler();

    public delegate void ExampleServiceProgressEventHandler(double progress);

    public delegate void ExampleServiceProcessingEventHandler(ProcessingStatus status);
}
