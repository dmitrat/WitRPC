using CommandLine;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.InterProcess.Model;

namespace OutWit.Examples.InterProcess.Shared
{
    public class StartupParametersTransport : AgentStartupParameters
    {
        #region Constructors


        public StartupParametersTransport(TransportType transportType, string address, int parentProcessId, TimeSpan timeout, bool shutdownOnParentProcessExited)
            : base(address, parentProcessId, timeout, shutdownOnParentProcessExited)
        {
            TransportType = transportType;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is StartupParametersTransport parameters))
                return false;

            return Address.Is(parameters.Address) &&
                   ParentProcessId.Is(parameters.ParentProcessId) &&
                   Timeout.Is(parameters.Timeout) &&
                   ShutdownOnParentProcessExited.Is(parameters.ShutdownOnParentProcessExited) &&
                   TransportType.Is(parameters.TransportType);
        }

        public override ModelBase Clone()
        {
            return new StartupParametersTransport(TransportType, Address, ParentProcessId, Timeout, ShutdownOnParentProcessExited);
        }

        #endregion

        #region Properties

        [Option("transport", Required = false, HelpText = "Transport type")]
        public TransportType TransportType { get;}

        #endregion
    }

    public enum TransportType
    {
        Tcp,
        Pipes,
        MMF,
        WebSocket
    }
}
