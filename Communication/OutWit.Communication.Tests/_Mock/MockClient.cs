using System;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Tests.Mock
{
    public class MockClient<TService> : IClient
        where TService : class
    {
        #region Events

        public event ClientEventHandler CallbackReceived = delegate { };

        #endregion

        #region Constructors

        public MockClient(TService service)
        {
            ParametersSerializer = new MessageSerializerJson();

            Processor = new RequestProcessor<TService>(service);
            Processor.ResetSerializer(ParametersSerializer);

            Processor.Callback += OnCallback;
        }

        #endregion

        #region IClient

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            return await Processor.Process(request);
        }

        #endregion

        #region Event Handlers

        private void OnCallback(WitRequest? request)
        {
            CallbackReceived(request);
        }

        #endregion

        #region Properties

        private IRequestProcessor Processor { get; }
        
        public IMessageSerializer ParametersSerializer { get; set; }

        #endregion
    }
}
