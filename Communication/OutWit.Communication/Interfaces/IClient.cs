using OutWit.Communication.Messages;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IClient
    {
        public event ClientEventHandler CallbackReceived;


        public Task<WitComResponse> SendRequest(WitComRequest? request);

        public IValueConverter Converter { get; }

        public bool IsInitialized { get; }

        public bool IsAuthorized { get;  }
    }

    public delegate void ClientEventHandler(WitComRequest? request);
}
