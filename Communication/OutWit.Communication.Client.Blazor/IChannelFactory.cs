namespace OutWit.Communication.Client.Blazor
{
    /// <summary>
    /// Factory for creating and managing WitRPC channel connections.
    /// Handles authentication state changes and automatic reconnection.
    /// </summary>
    public interface IChannelFactory : IAsyncDisposable
    {
        /// <summary>
        /// Gets a typed service proxy from the connected WitRPC channel.
        /// Ensures connection is established before returning the service.
        /// </summary>
        /// <typeparam name="T">Service interface type</typeparam>
        /// <returns>Service proxy instance</returns>
        Task<T> GetServiceAsync<T>() where T : class;

        /// <summary>
        /// Forces reconnection to the WitRPC server.
        /// Useful after authentication state changes.
        /// </summary>
        Task ReconnectAsync();
    }
}
