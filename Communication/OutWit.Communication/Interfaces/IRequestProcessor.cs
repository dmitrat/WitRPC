using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Messages;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Interfaces
{
    public interface IRequestProcessor
    {
        public event RequestProcessorEventHandler Callback;

        public Task<WitResponse> Process(WitRequest? request);
        
        public void ResetSerializer(IMessageSerializer serializer);
    }

    public delegate void RequestProcessorEventHandler(WitRequest? request);
}
