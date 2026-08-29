using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using OutWit.Common.CommandLine;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.InterProcess.Model;

namespace OutWit.InterProcess.Tests.Agent
{
    /// <summary>
    /// The real agent process the integration tests spawn: parses the startup
    /// parameters the host passes on the command line, serves
    /// <see cref="ITestAgentService"/> over the named pipe it was given, follows
    /// its parent process down, and leaves a marker file behind on a clean exit
    /// (a kill writes none -- that difference is what the graceful-stop test
    /// asserts).
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var parameters = args.DeserializeCommandLine<AgentStartupParameters>();

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                TryWriteGracefulMarker();

            WatchParent(parameters);

            using var server = WitServerBuilder.Build(options =>
            {
                options.WithNamedPipe(parameters.Address);
                options.WithService<ITestAgentService>(new TestAgentService());
            });

            server.StartWaitingForConnection();

            // The process lives until something shuts it down: the service's
            // scheduled exit, the parent dying, or the host killing it.
            Thread.Sleep(Timeout.Infinite);
            return 0;
        }

        private static void WatchParent(AgentStartupParameters parameters)
        {
            if (parameters.ParentProcessId == 0 || !parameters.ShutdownOnParentProcessExited)
                return;

            try
            {
                var parent = Process.GetProcessById(parameters.ParentProcessId);
                parent.EnableRaisingEvents = true;
                parent.Exited += (_, _) => Environment.Exit(0);
            }
            catch (Exception)
            {
                // The parent is already gone; there is nothing to serve.
                Environment.Exit(0);
            }
        }

        private static void TryWriteGracefulMarker()
        {
            try
            {
                File.WriteAllText(TestAgentMarker.PathFor(Environment.ProcessId), "graceful");
            }
            catch (Exception)
            {
                // A missing marker only fails the assertion, never the exit.
            }
        }
    }
}
