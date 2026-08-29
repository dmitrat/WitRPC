using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// Counts the requests that pass through it. Built with an inner handler for
    /// direct use, or without one for an IHttpClientFactory pipeline (which sets it).
    /// </summary>
    public sealed class MockHttpHandlerCounting : DelegatingHandler
    {
        #region Fields

        private int m_requests;

        #endregion

        #region Constructors

        public MockHttpHandlerCounting()
        {
        }

        public MockHttpHandlerCounting(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        #endregion

        #region Functions

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref m_requests);
            return base.SendAsync(request, cancellationToken);
        }

        #endregion

        #region Properties

        public int Requests => m_requests;

        #endregion
    }
}
