namespace OutWit.Communication.LoadTests.Contracts
{
    /// <summary>
    /// The contract between the load harness's server and its simulated nodes: a node attaches,
    /// receives tasks by event, acknowledges each one.
    /// </summary>
    public interface ILoadService
    {
        /// <summary>A task pushed to one node (targeted) or to all (broadcast).</summary>
        event Action<LoadTask> TaskReceived;

        /// <summary>
        /// Binds the calling connection to a node index; returns the connection id the server sees.
        /// </summary>
        Guid Attach(int nodeIndex);

        /// <summary>
        /// Acknowledges a task; the server measures the time from dispatch to this call. Async so
        /// that a node's callback handler does not hold a thread while the answer travels (a node
        /// that blocks per callback starves its own process long before the server is the limit).
        /// </summary>
        Task AckAsync(long taskId, int nodeIndex);
    }
}
