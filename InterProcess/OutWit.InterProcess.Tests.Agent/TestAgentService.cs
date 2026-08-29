using System;
using System.Threading.Tasks;

namespace OutWit.InterProcess.Tests.Agent
{
    /// <summary>
    /// The agent-side implementation. Exits are scheduled with a short delay so
    /// the reply to the triggering call still reaches the host.
    /// </summary>
    public sealed class TestAgentService : ITestAgentService
    {
        #region Constants

        private const int EXIT_DELAY_MS = 200;

        private const int CRASH_EXIT_CODE = 13;

        #endregion

        #region ITestAgentService

        public int GetProcessId()
        {
            return Environment.ProcessId;
        }

        public string Echo(string message)
        {
            return message;
        }

        public void Crash()
        {
            ScheduleExit(CRASH_EXIT_CODE, EXIT_DELAY_MS);
        }

        public void ExitAfter(int delayMs)
        {
            ScheduleExit(0, delayMs);
        }

        #endregion

        #region Tools

        private static void ScheduleExit(int exitCode, int delayMs)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                Environment.Exit(exitCode);
            });
        }

        #endregion
    }
}
