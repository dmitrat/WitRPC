namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// The default <see cref="IConnectionContextAccessor"/>: reads <see cref="ConnectionContext.Current"/>.
    /// Register it as a singleton; it holds no state of its own.
    /// </summary>
    public sealed class ConnectionContextAccessor : IConnectionContextAccessor
    {
        #region IConnectionContextAccessor

        /// <inheritdoc />
        public ConnectionContext? Current => ConnectionContext.Current;

        #endregion
    }
}
