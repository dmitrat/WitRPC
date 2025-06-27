using System;
using System.Threading.Tasks;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Interfaces
{
    public interface IClient
    {
        public event ClientEventHandler CallbackReceived;


        public Task<WitResponse> SendRequest(WitRequest? request);
        
        
        public IMessageSerializer ParametersSerializer { get; }
    }

    public delegate void ClientEventHandler(WitRequest? request);
}
