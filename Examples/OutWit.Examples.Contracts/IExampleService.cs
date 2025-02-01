using System.ComponentModel;
using OutWit.Common.Proxy.Attributes;

namespace OutWit.Examples.Contracts
{
    [ProxyTarget("ExampleServiceProxy")]
    public interface IExampleService
    {
        public event ExampleServiceEventHandler ProcessingStarted;
        public event ExampleServiceProgressEventHandler ProgressChanged;
        public event ExampleServiceProcessingEventHandler ProcessingCompleted;

        public bool StartProcessing();
        public Task<bool> StartProcessingAsync();

        public void StopProcessing();
        public Task StopProcessingAsync();
    }

    public delegate void ExampleServiceEventHandler();

    public delegate void ExampleServiceProgressEventHandler(double progress);

    public delegate void ExampleServiceProcessingEventHandler(ProcessingStatus status);
}
