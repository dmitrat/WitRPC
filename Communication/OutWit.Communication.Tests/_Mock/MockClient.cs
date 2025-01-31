using System;
using OutWit.Communication.Converters;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Processors;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

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
            Converter = new ValueConverterJson();

            Processor = new RequestProcessor<TService>(service);

            Processor.Callback += OnCallback;
        }

        #endregion

        #region IClient

        public async Task<WitComResponse> SendRequest(WitComRequest? request)
        {
            return await Processor.Process(request);
        }

        #endregion

        #region Event Handlers

        private void OnCallback(WitComRequest? request)
        {
            CallbackReceived(request);
        }

        #endregion

        #region Properties

        private IRequestProcessor Processor { get; }

        public IValueConverter Converter { get; set; }

        #endregion
    }
}
