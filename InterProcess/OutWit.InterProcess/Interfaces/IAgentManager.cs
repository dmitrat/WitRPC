using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.InterProcess.Interfaces
{
    public interface IAgentManager : IDisposable
    {
    }

    public interface IAgentManager<TService> : IAgentManager
        where TService : class
    {
    }
}
