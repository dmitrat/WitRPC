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
    public class HostAgent<TService> : IAgent<TService>
        where TService : class
    {
        #region Constants

        private const int DISCONNECT_TIMEOUT_SEC = 1;

        #endregion

        #region Events

        public event AgentEventHandler Initialized = delegate { };

        public event AgentEventHandler Disposed = delegate { };

        #endregion

        #region Constructors

        public HostAgent()
        {
            Id = Guid.NewGuid();
        }

        #endregion

        #region IAgent

        public bool Start(WitClientBuilderOptions options, string pathToService, TimeSpan timeout)
        {
            if(!RunProcess(options, pathToService, timeout))
                return false;

            try
            {
                Client = WitClientBuilder.Build(options);
            }
            catch (Exception e)
            {
                Dispose();
                return false;
            }

            return true;
        }

        public async Task Stop()
        {
            if(Client == null)
                return;

            await Client.Disconnect();
            Client = null;

            Dispose();
        }

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
            catch (Exception e)
            {
                Dispose();
                return false;
            }

            IsInitialized = true;
            Initialized(Id);

            return true;
        }

        public void Shutdown()
        {
            Process?.Kill();
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
            catch (Exception e)
            {
                Process = null;
                return false;
            }
   
            return true;
        }


        #endregion

        #region Event Handlers

        private void OnProcessExited(object? sender, EventArgs e)
        {
            IsInitialized = false;
            Service = null;
            Client = null;
            Process = null;

            Disposed(Id);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                IsInitialized = false;
                Service = null;

                Client?.Disconnect().Wait(TimeSpan.FromSeconds(DISCONNECT_TIMEOUT_SEC));
                Client = null;

                Process?.Kill();
                Process = null;

            }
            catch (Exception e)
            {
                

            }
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
