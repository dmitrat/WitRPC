using System;

namespace OutWit.InterProcess.Interfaces
{
    public interface IAgent<out TService> : IDisposable
        where TService : class
    {
        public event AgentEventHandler Initialized;
        public event AgentEventHandler Disposed;


        public Task Stop();

        public Task<bool> Initialize(TimeSpan timeout);

        public void Shutdown();


        public TService? Service { get; }

        public bool IsInitialized { get; }

        public Guid Id { get; }
    }

    public delegate void AgentEventHandler(Guid id);
}
