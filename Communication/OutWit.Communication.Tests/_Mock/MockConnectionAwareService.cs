using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using OutWit.Communication.Server.Callbacks;
using OutWit.Communication.Server.Connections;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// The service behind <see cref="IConnectionAwareService"/>. It keeps the delivery reports
    /// of its targeted raises so a test that holds the instance can read them.
    /// </summary>
    public sealed class MockConnectionAwareService : IConnectionAwareService
    {
        #region Events

        public event Action<string> Notified = delegate { };

        #endregion

        #region IConnectionAwareService

        public Guid WhoAmI()
        {
            return ConnectionContext.Current?.ConnectionId ?? Guid.Empty;
        }

        public string? WhoAmIPrincipal()
        {
            return ConnectionContext.Current?.Principal?.FindFirst(ClaimTypes.Name)?.Value;
        }

        public Guid WhichServer()
        {
            return ConnectionContext.Current?.ServerId ?? Guid.Empty;
        }

        public Guid WhoAmIAfterAwait()
        {
            return WhoAmIAfterAwaitAsync().GetAwaiter().GetResult();
        }

        public void NotifyAll(string message)
        {
            Notified(message);
        }

        public void NotifyCaller(string message)
        {
            using var scope = CallbackScope.TargetCaller();
            Notified(message);
            Reports.Enqueue(scope.Completion);
        }

        public void NotifyOne(Guid connectionId, string message)
        {
            using var scope = CallbackScope.Target(connectionId);
            Notified(message);
            Reports.Enqueue(scope.Completion);
        }

        public void NotifyMany(Guid[] connectionIds, string message)
        {
            using var scope = CallbackScope.Target(connectionIds);
            Notified(message);
            Reports.Enqueue(scope.Completion);
        }

        #endregion

        #region Tools

        private static async Task<Guid> WhoAmIAfterAwaitAsync()
        {
            await Task.Delay(10).ConfigureAwait(false);
            return ConnectionContext.Current?.ConnectionId ?? Guid.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The completion of every targeted raise, in call order.
        /// </summary>
        public ConcurrentQueue<Task<CallbackDeliveryReport>> Reports { get; } = new();

        #endregion
    }
}
