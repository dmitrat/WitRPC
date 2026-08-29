using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client.Rest
{
    /// <summary>
    /// The REST transport client. Each call is one stateless HTTP request: the
    /// whole <see cref="WitRequest"/> is the JSON body of a
    /// <c>POST {base}/{method}</c> (or a <c>GET</c> for a parameterless method
    /// when the mode allows one), and the reply is always a <see cref="WitResponse"/>
    /// read back from the body — including on a non-2xx status, so a server fault
    /// comes back as a response rather than a thrown HTTP exception.
    /// </summary>
    public class WitClientRest : IClient
    {
        #region Constants

        private const string JSON_MEDIA_TYPE = "application/json";

        #endregion

        #region Events

        public event ClientEventHandler CallbackReceived = delegate { };

        #endregion

        #region Fields

        // One HttpClient for the whole process, as intended: it pools connections
        // and is thread-safe. Its own timeout is disabled; each call is bounded by
        // the options timeout through a linked token instead.
        private static readonly HttpClient s_http = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

        #endregion

        #region Constructors

        public WitClientRest(RestClientTransportOptions options, IAccessTokenProvider tokenProvider)
        {
            if (string.IsNullOrEmpty(options.Host?.Connection))
                throw new WitException("Url cannot be empty");

            Options = options;
            ParametersSerializer = new MessageSerializerJson();
            MessageSerializer = new MessageSerializerJson();
            TokenProvider = tokenProvider;
        }

        #endregion

        #region IClient

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            if (request == null)
                return WitResponse.BadRequest("Empty request");

            try
            {
                request.Token = await TokenProvider.GetToken();
            }
            catch (Exception e)
            {
                return WitResponse.TransportError("Failed to obtain the access token", e);
            }

            using var httpRequest = BuildRequest(request);
            using var timeout = CreateTimeout();

            try
            {
                using var httpResponse = await s_http
                    .SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, timeout?.Token ?? CancellationToken.None)
                    .ConfigureAwait(false);

                byte[] body = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                if (body.Length == 0)
                    return WitResponse.TransportError($"Server returned an empty body (HTTP {(int)httpResponse.StatusCode})");

                return MessageSerializer.Deserialize<WitResponse>(body)
                       ?? WitResponse.TransportError("Failed to deserialize the response");
            }
            catch (OperationCanceledException) when (timeout is { IsCancellationRequested: true })
            {
                return WitResponse.Timeout("REST request timed out");
            }
            catch (Exception e)
            {
                return WitResponse.TransportError("REST request failed", e);
            }
        }

        #endregion

        #region Functions

        private HttpRequestMessage BuildRequest(WitRequest request)
        {
            var uri = new Uri(Options.Host!.AppendPath(request.MethodName).BuildConnection(true));

            bool parameterless = request.Parameters.Length == 0;
            bool useGet = parameterless && Options.Mode != RestClientRequestModes.PostOnly;

            HttpRequestMessage message;
            if (useGet)
            {
                message = new HttpRequestMessage(HttpMethod.Get, uri);
            }
            else
            {
                message = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new ByteArrayContent(MessageSerializer.Serialize(request))
                };
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(JSON_MEDIA_TYPE);
            }

            if (!string.IsNullOrEmpty(request.Token))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);

            return message;
        }

        private CancellationTokenSource? CreateTimeout()
        {
            if (Options.Timeout == null || Options.Timeout == TimeSpan.Zero)
                return null;

            return new CancellationTokenSource(Options.Timeout.Value);
        }

        #endregion

        #region Properties

        public RestClientTransportOptions Options { get; }

        public IMessageSerializer ParametersSerializer { get; }

        private IMessageSerializer MessageSerializer { get; }

        public IAccessTokenProvider TokenProvider { get; }

        #endregion
    }
}
