using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface ITransportServerFactory
    {
        public event TransportFactoryEventHandler NewClientConnected;

        public void StartWaitingForConnection();

        public void StopWaitingForConnection();
    }

    public delegate void TransportFactoryEventHandler(ITransportServer transport);
}
