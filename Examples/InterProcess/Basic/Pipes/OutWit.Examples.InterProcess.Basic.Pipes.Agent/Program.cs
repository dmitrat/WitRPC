using System;
using OutWit.Common.CommandLine;
using OutWit.Examples.InterProcess.Basic.Pipes.Agent.ViewModels;
using OutWit.InterProcess.Model;

namespace OutWit.Examples.InterProcess.Basic.Pipes.Agent
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationViewModel.Instance.Run(args.DeserializeCommandLine<AgentStartupParameters>());
        }
    }
}
