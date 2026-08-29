using OutWit.InterProcess.Host;
using OutWit.InterProcess.Tests.Agent;

namespace OutWit.InterProcess.Tests
{
    /// <summary>
    /// The synchronized agent registry against real processes: every agent on
    /// its own endpoint, self-removal on death, creation after a crash, safe
    /// concurrent creation, and a disposal that leaves no process running.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public sealed class HostManagerTests
    {
        #region Constants

        private static readonly TimeSpan INITIALIZE_TIMEOUT = TimeSpan.FromSeconds(15);

        private static readonly TimeSpan EXIT_WAIT = TimeSpan.FromSeconds(10);

        #endregion

        #region Registry Tests

        [Test]
        public async Task CreateShutdownAndDisposeCleanEverythingTest()
        {
            using var manager = CreateManager();

            var first = await manager.CreateClient(INITIALIZE_TIMEOUT);
            var second = await manager.CreateClient(INITIALIZE_TIMEOUT);

            // Two agents, two processes, two distinct endpoints.
            int firstProcessId = first.Service!.GetProcessId();
            int secondProcessId = second.Service!.GetProcessId();
            Assert.That(firstProcessId, Is.Not.EqualTo(secondProcessId));

            Assert.That(manager.GetAgent(first.Id), Is.SameAs(first));

            // A hard shutdown removes the agent from the registry through its
            // own Disposed event.
            Assert.That(manager.ShutdownAgent(first.Id), Is.True);
            Assert.That(HostAgentTests.WaitForProcessGone(firstProcessId), Is.True);
            Assert.That(SpinWait.SpinUntil(() => manager.GetAgent(first.Id) == null, EXIT_WAIT), Is.True);

            // Disposing the manager takes the remaining process down with it.
            manager.Dispose();
            Assert.That(HostAgentTests.WaitForProcessGone(secondProcessId), Is.True);
        }

        [Test]
        public async Task AgentCrashRemovesItFromTheRegistryTest()
        {
            using var manager = CreateManager();

            var agent = await manager.CreateClient(INITIALIZE_TIMEOUT);
            int processId = agent.Service!.GetProcessId();

            agent.Service.Crash();

            Assert.That(HostAgentTests.WaitForProcessGone(processId), Is.True);
            Assert.That(SpinWait.SpinUntil(() => manager.GetAgent(agent.Id) == null, EXIT_WAIT), Is.True,
                "a crashed agent must remove itself from the registry");

            // The host recovers by simply creating a replacement.
            var replacement = await manager.CreateClient(INITIALIZE_TIMEOUT);
            Assert.That(replacement.Service!.Echo("recovered"), Is.EqualTo("recovered"));
        }

        [Test]
        public async Task ConcurrentCreationRegistersEveryAgentTest()
        {
            using var manager = CreateManager();

            var agents = await Task.WhenAll(
                manager.CreateClient(INITIALIZE_TIMEOUT),
                manager.CreateClient(INITIALIZE_TIMEOUT),
                manager.CreateClient(INITIALIZE_TIMEOUT));

            Assert.That(agents.Select(agent => agent.Id).Distinct().Count(), Is.EqualTo(3));

            foreach (var agent in agents)
            {
                Assert.That(manager.GetAgent(agent.Id), Is.SameAs(agent));
                Assert.That(agent.Service!.Echo("hello"), Is.EqualTo("hello"));
            }
        }

        #endregion

        #region Helpers

        private static HostManager<ITestAgentService> CreateManager()
        {
            // The factory hands every agent its own pipe -- endpoints are
            // per-process, never shared.
            return new HostManager<ITestAgentService>(
                () => HostAgentTests.CreateOptions(HostAgentTests.NextPipe()),
                HostAgentTests.AgentPath,
                TimeSpan.Zero);
        }

        #endregion
    }
}
