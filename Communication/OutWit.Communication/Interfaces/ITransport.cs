using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface ITransport : IDisposable
    {
        event TransportDataEventHandler Callback;

        event TransportEventHandler Disconnected;

        public Task SendBytesAsync(byte[] data);

        public Guid Id { get; }
    }

    public delegate void TransportDataEventHandler(Guid sender, byte[] data);
    public delegate void TransportEventHandler(Guid sender);
}
