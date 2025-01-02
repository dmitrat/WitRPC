using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using OutWit.InterProcess.Model;

namespace OutWit.InterProcess.Agent
{
    public class AgentApplication : Application
    {
        #region Constructors

        public AgentApplication(AgentStartupParameters parameters)
        {
            Parameters = parameters;

            InitEvents();
            ResetTimeout();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            if(Parameters.ParentProcessId == 0 || !Parameters.ShutdownOnParentProcessExited)
                return;

            Process? process = null;
            try
            {
                process = Process.GetProcessById(Parameters.ParentProcessId);
                process.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Shutdown(0);
            }

            if(process != null)
                process.Exited += OnParentProcessExited;
        }

        #endregion

        #region Functions

        public void ResetTimeout()
        {
            Timer?.Stop();

            if(Parameters.Timeout == TimeSpan.Zero)
                return;

            Timer = new DispatcherTimer { Interval = Parameters.Timeout };
            Timer.Tick += OnTimeout;
            Timer.Start();
        }

        #endregion

        #region Event Handlers

        private void OnTimeout(object? sender, EventArgs e)
        {
            Shutdown(0);
        }

        private void OnParentProcessExited(object? sender, EventArgs e)
        {
            Shutdown(0);
        }

        #endregion

        #region Properties

        private AgentStartupParameters Parameters { get; }

        private DispatcherTimer? Timer { get; set; }

        #endregion
    }
}
