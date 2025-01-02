using System;
using OutWit.Common.Abstract;
using OutWit.Common.Random;
using OutWit.Common.Values;
using System.Diagnostics;
using OutWit.Common.CommandLine;
using CommandLine;

namespace OutWit.InterProcess.Model
{
    public class AgentStartupParameters : ModelBase
    {
        #region Constructors

        private AgentStartupParameters()
        {
            Address = "";
            ParentProcessId = 0;
            Timeout = TimeSpan.Zero;
        }

        public AgentStartupParameters(string address, int parentProcessId, TimeSpan timeout, bool shutdownOnParentProcessExited)
        {
            Address = address;
            ParentProcessId = parentProcessId;
            Timeout = timeout;
            ShutdownOnParentProcessExited = shutdownOnParentProcessExited;
        }


        public AgentStartupParameters(string address, TimeSpan timeout, bool shutdownOnParentProcessExited = true)
            : this(address, Process.GetCurrentProcess().Id, timeout, shutdownOnParentProcessExited)
        {
        }

        public AgentStartupParameters(TimeSpan timeout, bool shutdownOnParentProcessExited = true)
        : this(RandomUtils.RandomString(), Process.GetCurrentProcess().Id, timeout, shutdownOnParentProcessExited)
        {
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return this.SerializeCommandLine();
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is AgentStartupParameters parameters))
                return false;

            return Address.Is(parameters.Address) &&
                   ParentProcessId.Is(parameters.ParentProcessId) &&
                   Timeout.Is(parameters.Timeout) &&
                   ShutdownOnParentProcessExited.Is(parameters.ShutdownOnParentProcessExited);
        }

        public override ModelBase Clone()
        {
            return new AgentStartupParameters(Address, ParentProcessId, Timeout, ShutdownOnParentProcessExited);
        }

        #endregion

        #region Properties

        [Option('a', "address", Required = true, HelpText = "Transport name or address")]
        public string Address { get; }

        [Option('p', "process", Required = false, HelpText = "Parent process id")]
        public int ParentProcessId { get; }

        [Option('t', "timeout", Required = false, HelpText = "Timeout for agent before automatically shutdown")]
        public TimeSpan Timeout { get; }

        [Option('s', "shutdown", Required = false, HelpText = "Shutdown on parent process exited")]
        public bool ShutdownOnParentProcessExited { get; }

        #endregion
    }
}
