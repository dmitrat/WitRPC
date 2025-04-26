using System;
using System.Threading.Tasks;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Interfaces
{
    public interface IClient
    {
        public event ClientEventHandler CallbackReceived;


        public Task<WitComResponse> SendRequest(WitComRequest? request);
        
        
        public IMessageSerializer Serializer { get; }
    }

    public delegate void ClientEventHandler(WitComRequest? request);
}
