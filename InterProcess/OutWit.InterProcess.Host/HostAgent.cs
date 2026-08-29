using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Client;
using OutWit.InterProcess.Host.Utils;
using OutWit.InterProcess.Interfaces;
using OutWit.InterProcess.Model;

namespace OutWit.InterProcess.Host
{
    /// <summary>
    /// One out-of-process agent as the host sees it: the spawned process and
    /// the <see cref="WitClient"/> talking to it. Stopping is graceful --
    /// disconnect, a bounded wait for the process to leave on its own, and only
    /// then a kill of the whole process tree; disposal always releases the
    /// client, the process handle and the event subscriptions, whichever side
    /// died first.
    /// </summary>
    public class HostAgent<TService> : IAgent<TService>
        where TService : class
    {
        #region Constants

        private static readonly TimeSpan DISCONNECT_TIMEOUT = TimeSpan.FromSeconds(1);

        private static readonly TimeSpan GRACEFUL_EXIT_TIMEOUT = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan KILL_EXIT_TIMEOUT = TimeSpan.FromSeconds(5);

        #endregion

        #region Events

        public event AgentEventHandler Initialized = delegate { };

        public event AgentEventHandler Disposed = delegate { };

        #endregion

        #region Fields

        private readonly object m_lock = new();

        private int m_disposedRaised;

        #endregion

        #region Constructors

        public HostAgent()
        {
            Id = Guid.NewGuid();
        }

        #endregion

        #region IAgent

        /// <summary>
        /// Spawns the agent process and prepares the client for it. On any
        /// failure the process is torn down again -- a false return leaves
        /// nothing behind.
        /// </summary>
        /// <param name="options">Client options; the transport's address is handed to the agent.</param>
        /// <param name="pathToService">Path to the agent executable.</param>
        /// <param name="timeout">The agent's idle-shutdown timeout (zero for none).</param>
        /// <returns>True when the process is running and the client is built.</returns>
        public bool Start(WitClientBuilderOptions options, string pathToService, TimeSpan timeout)
        {
            if (!RunProcess(options, pathToService, timeout))
                return false;

            try
            {
                Client = WitClientBuilder.Build(options);
            }
            catch (Exception)
            {
                Dispose();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Graceful shutdown: disconnect, give the agent a bounded chance to
        /// exit on its own, and kill the process tree only if it does not.
        /// </summary>
        public async Task Stop()
        {
            WitClient? client;
            Process? process;

            lock (m_lock)
            {
                client = Client;
                process = Process;
            }

            if (client != null)
            {
                try
                {
                    await client.Disconnect();
                }
                catch (Exception)
                {
                    // The connection may already be gone; the wait below decides.
                }
            }

            if (process != null && !await WaitForExitAsync(process, GRACEFUL_EXIT_TIMEOUT))
                KillProcess(process);

            Dispose();
        }

        /// <summary>
        /// Connects to the agent and builds the service proxy. On any failure
        /// the agent is torn down -- a false return leaves nothing behind.
        /// </summary>
        /// <param name="timeout">How long to wait for the agent's endpoint to come up.</param>
        /// <returns>True when the service proxy is ready.</returns>
        public async Task<bool> Initialize(TimeSpan timeout)
        {
            if (Process == null || Client == null)
                return false;

            var result = await Client.ConnectAsync(timeout, CancellationToken.None);
            if (!result)
            {
                Dispose();
                return false;
            }

            try
            {
                Service = Client.GetService<TService>();
            }
            catch (Exception)
            {
                Dispose();
                return false;
            }

            IsInitialized = true;
            Initialized(Id);

            return true;
        }

        /// <summary>The kill switch: no disconnect, the whole process tree goes down now.</summary>
        public void Shutdown()
        {
            Process? process;

            lock (m_lock)
                process = Process;

            if (process != null)
                KillProcess(process);

            Dispose();
        }

        #endregion

        #region Process

        private bool RunProcess(WitClientBuilderOptions options, string pathToService, TimeSpan timeout)
        {
            if (!File.Exists(pathToService))
                return false;

            try
            {
                var parameters = new AgentStartupParameters(options.Transport?.Address ?? "", timeout);
                var process = HostUtils.RunAgent(pathToService, parameters);
                if (process == null)
                    return false;

                Process = process;
                Process.Exited += OnProcessExited;
            }
            catch (Exception)
            {
                Process = null;
                return false;
            }

            return true;
        }

        private static void KillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);

                process.WaitForExit((int)KILL_EXIT_TIMEOUT.TotalMilliseconds);
            }
            catch (Exception)
            {
                // Already exited, or the handle is gone -- either way it is down.
            }
        }

        private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
        {
            try
            {
                if (process.HasExited)
                    return true;

                using var cancellation = new CancellationTokenSource(timeout);
                await process.WaitForExitAsync(cancellation.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        #endregion

        #region Tools

        /// <summary>
        /// Releases everything exactly once per resource: the client is disposed
        /// (not just disconnected), the process is unhooked, killed if still
        /// alive, and its handle disposed. Safe to call from any path, any
        /// number of times, concurrently.
        /// </summary>
        private void CleanUp(bool killProcess)
        {
            WitClient? client;
            Process? process;

            lock (m_lock)
            {
                client = Client;
                process = Process;

                Client = null;
                Process = null;
                Service = null;
                IsInitialized = false;
            }

            if (client != null)
            {
                try
                {
                    client.Disconnect().Wait(DISCONNECT_TIMEOUT);
                }
                catch (Exception)
                {
                    // The transport may already be down.
                }

                client.Dispose();
            }

            if (process != null)
            {
                process.Exited -= OnProcessExited;

                if (killProcess)
                    KillProcess(process);

                process.Dispose();
            }
        }

        private void RaiseDisposed()
        {
            if (Interlocked.Exchange(ref m_disposedRaised, 1) != 0)
                return;

            Disposed(Id);
        }

        #endregion

        #region Event Handlers

        private void OnProcessExited(object? sender, EventArgs e)
        {
            CleanUp(killProcess: false);
            RaiseDisposed();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            CleanUp(killProcess: true);
            RaiseDisposed();
        }

        #endregion

        #region Properties

        private Process? Process { get; set; }

        private WitClient? Client { get; set; }


        public TService? Service { get; private set; }

        public bool IsInitialized { get; private set; }


        public Guid Id { get; }

        #endregion
    }
}
