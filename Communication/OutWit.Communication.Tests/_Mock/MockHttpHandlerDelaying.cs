using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// Holds every request for a while before forwarding it, honouring the
    /// cancellation token -- so an HttpClient's own timeout has something to cut short.
    /// </summary>
    public sealed class MockHttpHandlerDelaying : DelegatingHandler
    {
        #region Constructors

        public MockHttpHandlerDelaying(TimeSpan delay, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            Delay = delay;
        }

        #endregion

        #region Functions

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }

        #endregion

        #region Properties

        public TimeSpan Delay { get; }

        #endregion
    }
}
