using System;
using System.Diagnostics;
using OutWit.InterProcess.Model;

namespace OutWit.InterProcess.Host.Utils
{
    public static class HostUtils
    {
        public static Process? RunAgent(string pathToAgent, string address, TimeSpan timeout, bool shutdownOnParentProcessExited)
        {
            var parameters = new AgentStartupParameters(address, timeout, shutdownOnParentProcessExited);
            return RunAgent(pathToAgent, parameters);
        }

        public static Process? RunAgent(string pathToAgent, string address)
        {
            return RunAgent(pathToAgent, address, TimeSpan.Zero, true);
        }

        public static Process? RunAgent<T>(string pathToAgent, T parameters)
            where T: AgentStartupParameters
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToAgent,
                    Arguments = parameters.ToString(),
                    CreateNoWindow = true
                }
            };

            return !process.Start() ? null : process;
        }
    }
}
