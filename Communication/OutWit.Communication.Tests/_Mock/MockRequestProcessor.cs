using System;
using OutWit.Common.Json;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Tests.Mock
{
    public class MockRequestProcessor : IRequestProcessor
    {
        public event RequestProcessorEventHandler Callback = delegate { };

        public async Task<WitResponse> Process(WitRequest? request)
        {
            Thread.Sleep(50);
            
            return WitResponse.Success(request?.MethodName.ToJsonBytes());
        }

        public void ResetSerializer(IMessageSerializer serializer)
        {
            Serializer = serializer;
        }

        public void InvokeCallback(WitRequest request)
        {
            Callback(request);
        }
        
        public IMessageSerializer? Serializer { get; set; }
    }
}
