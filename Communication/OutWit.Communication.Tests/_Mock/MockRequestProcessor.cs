using System;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Tests.Mock
{
    public class MockRequestProcessor : IRequestProcessor
    {
        public event RequestProcessorEventHandler Callback = delegate { };

        public async Task<WitComResponse> Process(WitComRequest? request)
        {
            Thread.Sleep(50);
            
            return WitComResponse.Success(Array.Empty<byte>());
        }

        public void ResetSerializer(IMessageSerializer serializer)
        {
            Serializer = serializer;
        }

        public void InvokeCallback(WitComRequest request)
        {
            Callback(request);
        }
        
        public IMessageSerializer? Serializer { get; set; }
    }
}
