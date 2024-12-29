using System;
using OutWit.Common.CommandLine;
using OutWit.Examples.InterProcess.BasicAgent.ViewModels;
using OutWit.Examples.InterProcess.Shared;

namespace OutWit.Examples.InterProcess.BasicAgent
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationViewModel.Instance.Run(args.DeserializeCommandLine<StartupParametersTransport>());
        }
    }
}
