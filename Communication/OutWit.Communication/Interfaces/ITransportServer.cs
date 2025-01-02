using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface ITransportServer : ITransport
    {
        Task<bool> InitializeConnectionAsync(CancellationToken token);

        bool CanReinitialize { get; }
    }
}
