namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// Gives a service the <see cref="ConnectionContext"/> of the request it is handling by
    /// injection rather than through the static <see cref="ConnectionContext.Current"/>, so a
    /// unit test can hand the service a fake.
    /// </summary>
    public interface IConnectionContextAccessor
    {
        /// <summary>
        /// The context of the request being processed on this async flow, or null outside a request.
        /// </summary>
        ConnectionContext? Current { get; }
    }
}
