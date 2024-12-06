using System;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Tests.Mock
{
    public class MockRequestProcessor : IRequestProcessor
    {
        public event RequestProcessorEventHandler Callback = delegate { };

        public WitComResponse Process(WitComRequest? request)
        {
            Thread.Sleep(50);
            return WitComResponse.Success(request?.MethodName);
        }

        public void InvokeCallback(WitComRequest request)
        {
            Callback(request);
        }
    }
}
