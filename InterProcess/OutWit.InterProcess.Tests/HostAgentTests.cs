using System.Diagnostics;
using OutWit.Communication.Client;
using OutWit.Communication.Client.Pipes.Utils;
using OutWit.InterProcess.Host;
using OutWit.InterProcess.Tests.Agent;

namespace OutWit.InterProcess.Tests
{
    /// <summary>
    /// Integration tests against a real agent process: start, call, crash,
    /// graceful stop versus kill, and the guarantee that whatever happened, no
    /// process is left behind.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public sealed class HostAgentTests
    {
        #region Constants

        private static readonly TimeSpan INITIALIZE_TIMEOUT = TimeSpan.FromSeconds(15);

        private static readonly TimeSpan EXIT_WAIT = TimeSpan.FromSeconds(10);

        #endregion

        #region Lifecycle Tests

        [Test]
        public async Task StartInitializeCallAndStopTest()
        {
            var agent = new HostAgent<ITestAgentService>();

            Assert.That(agent.Start(CreateOptions(NextPipe()), AgentPath, TimeSpan.Zero), Is.True);
            Assert.That(await agent.Initialize(INITIALIZE_TIMEOUT), Is.True);
            Assert.That(agent.IsInitialized, Is.True);

            var service = agent.Service!;
            Assert.That(service.Echo("ping"), Is.EqualTo("ping"));

            int processId = service.GetProcessId();
            Assert.That(processId, Is.GreaterThan(0));

            await agent.Stop();

            Assert.That(agent.IsInitialized, Is.False);
            Assert.That(agent.Service, Is.Null);
            Assert.That(WaitForProcessGone(processId), Is.True, "the agent process must not outlive Stop");
        }

        [Test]
        public async Task StopIsGracefulWhenAgentExitsOnItsOwnTest()
        {
            var agent = new HostAgent<ITestAgentService>();

            Assert.That(agent.Start(CreateOptions(NextPipe()), AgentPath, TimeSpan.Zero), Is.True);
            Assert.That(await agent.Initialize(INITIALIZE_TIMEOUT), Is.True);

            int processId = agent.Service!.GetProcessId();
            string marker = TestAgentMarker.PathFor(processId);
            File.Delete(marker);

            // The agent will leave on its own shortly; Stop's bounded wait must
            // let it, and the clean exit is what writes the marker -- a kill
            // would not.
            agent.Service.ExitAfter(500);
            await agent.Stop();

            Assert.That(WaitForProcessGone(processId), Is.True);
            Assert.That(SpinWait.SpinUntil(() => File.Exists(marker), EXIT_WAIT), Is.True,
                "no graceful-exit marker: the process was killed instead of being allowed to leave");

            File.Delete(marker);
        }

        [Test]
        public async Task ShutdownKillsTheProcessTest()
        {
            var agent = new HostAgent<ITestAgentService>();

            Assert.That(agent.Start(CreateOptions(NextPipe()), AgentPath, TimeSpan.Zero), Is.True);
            Assert.That(await agent.Initialize(INITIALIZE_TIMEOUT), Is.True);

            int processId = agent.Service!.GetProcessId();
            string marker = TestAgentMarker.PathFor(processId);
            File.Delete(marker);

            agent.Shutdown();

            Assert.That(WaitForProcessGone(processId), Is.True);
            Assert.That(File.Exists(marker), Is.False,
                "a killed process must not have run its clean-exit path");
        }

        [Test]
        public async Task CrashRaisesDisposedAndCleansUpTest()
        {
            var agent = new HostAgent<ITestAgentService>();

            Assert.That(agent.Start(CreateOptions(NextPipe()), AgentPath, TimeSpan.Zero), Is.True);
            Assert.That(await agent.Initialize(INITIALIZE_TIMEOUT), Is.True);

            var disposedRaised = new ManualResetEventSlim(false);
            agent.Disposed += _ => disposedRaised.Set();

            int processId = agent.Service!.GetProcessId();
            agent.Service.Crash();

            Assert.That(disposedRaised.Wait(EXIT_WAIT), Is.True, "a dead agent must announce itself");
            Assert.That(WaitForProcessGone(processId), Is.True);
            Assert.That(agent.IsInitialized, Is.False);
            Assert.That(agent.Service, Is.Null);
        }

        [Test]
        public void StartFailsCleanlyWhenAgentIsMissingTest()
        {
            var agent = new HostAgent<ITestAgentService>();

            Assert.That(agent.Start(CreateOptions(NextPipe()), @"C:\does\not\exist.exe", TimeSpan.Zero), Is.False);
            Assert.That(agent.IsInitialized, Is.False);
        }

        #endregion

        #region Helpers

        internal static string AgentPath =>
            Path.Combine(
                AppContext.BaseDirectory.Replace("OutWit.InterProcess.Tests", "OutWit.InterProcess.Tests.Agent"),
                "OutWit.InterProcess.Tests.Agent.exe");

        internal static string NextPipe()
        {
            return $"WitRpcTestAgent_{Guid.NewGuid():N}";
        }

        internal static WitClientBuilderOptions CreateOptions(string pipe)
        {
            var options = new WitClientBuilderOptions();
            options.WithNamedPipe(pipe);
            return options;
        }

        internal static bool WaitForProcessGone(int processId)
        {
            return SpinWait.SpinUntil(() => IsProcessGone(processId), EXIT_WAIT);
        }

        private static bool IsProcessGone(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return process.HasExited;
            }
            catch (ArgumentException)
            {
                return true;
            }
        }

        #endregion
    }
}
