using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Rest
{
    /// <summary>
    /// The REST transport server. It hosts one HTTP endpoint per service: a
    /// <c>POST {base}/{method}</c> whose JSON body is the whole
    /// <see cref="WitRequest"/> (or a <c>GET {base}/{method}</c> for a
    /// parameterless call), runs it through the same <see cref="IRequestProcessor"/>
    /// every other transport uses, and returns the <see cref="WitResponse"/> as
    /// JSON with an HTTP status mapped from it.
    /// <para>
    /// Requests are handled concurrently and independently: a slow or failing one
    /// neither blocks the accept loop nor brings the listener down. The body is
    /// size-capped and processing is time-bounded.
    /// </para>
    /// <para>
    /// REST is stateless request/reply, so it carries no server-to-client
    /// callbacks; events are delivered only by the persistent transports.
    /// </para>
    /// </summary>
    public class WitServerRest : IDisposable
    {
        #region Constants

        private const string JSON_MEDIA_TYPE = "application/json";

        public const long DEFAULT_MAX_BODY_BYTES = 64L * 1024 * 1024;

        #endregion

        #region Fields

        private readonly SemaphoreSlim m_limit;

        private bool m_isDisposed;

        #endregion

        #region Constructors

        public WitServerRest(RestServerTransportOptions options, IAccessTokenValidator tokenValidator, IRequestProcessor requestProcessor,
            ILogger? logger, TimeSpan? timeout, long maxBodyBytes = DEFAULT_MAX_BODY_BYTES, int maxConcurrentRequests = int.MaxValue)
        {
            Options = options;
            Serializer = new MessageSerializerJson();
            TokenValidator = tokenValidator;
            RequestProcessor = requestProcessor;

            RequestProcessor.ResetSerializer(Serializer);

            Logger = logger;
            Timeout = timeout;
            MaxBodyBytes = maxBodyBytes;

            m_limit = new SemaphoreSlim(Math.Max(1, maxConcurrentRequests));
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            if (Listener != null)
                return;

            Listener = new HttpListener();
            Listener.Prefixes.Add(Options.Host!.BuildConnection());
            TokenSource = new CancellationTokenSource();

            Listener.Start();

            var listener = Listener;
            var token = TokenSource.Token;
            Task.Run(() => AcceptLoopAsync(listener, token));
        }

        public void StopWaitingForConnection()
        {
            Dispose();
        }

        private async Task AcceptLoopAsync(HttpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                    return;

                // Handle off the accept loop: one slow or failing request must
                // not hold up the next, and must never take the listener down.
                _ = HandleAsync(context);
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                await m_limit.WaitAsync().ConfigureAwait(false);

                try
                {
                    var (response, status) = await ProcessAsync(context.Request).ConfigureAwait(false);
                    WriteResponse(context.Response, response, status);
                }
                finally
                {
                    m_limit.Release();
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to handle a REST request");
                TryWriteError(context.Response);
            }
        }

        private async Task<(WitResponse response, HttpStatusCode status)> ProcessAsync(HttpListenerRequest httpRequest)
        {
            string? token = GetBearerToken(httpRequest);

            if (!TokenValidator.IsRequestTokenValid(token ?? ""))
                return (WitResponse.UnauthorizedRequest("Token is not valid"), HttpStatusCode.Unauthorized);

            WitRequest? request;

            if (httpRequest.HttpMethod == HttpMethod.Post.Method)
            {
                if (httpRequest.ContentLength64 > MaxBodyBytes)
                    return (WitResponse.BadRequest($"Request body exceeds the {MaxBodyBytes} byte limit"), HttpStatusCode.RequestEntityTooLarge);

                byte[]? body = await ReadBodyAsync(httpRequest).ConfigureAwait(false);
                if (body == null)
                    return (WitResponse.BadRequest($"Request body exceeds the {MaxBodyBytes} byte limit"), HttpStatusCode.RequestEntityTooLarge);

                try
                {
                    request = Serializer.Deserialize<WitRequest>(body);
                }
                catch (Exception e)
                {
                    return (WitResponse.BadRequest("Failed to parse the request", e), HttpStatusCode.BadRequest);
                }
            }
            else if (httpRequest.HttpMethod == HttpMethod.Get.Method)
            {
                request = new WitRequest { MethodName = MethodFromUrl(httpRequest) };
            }
            else
            {
                return (WitResponse.BadRequest($"Unsupported method {httpRequest.HttpMethod}"), HttpStatusCode.MethodNotAllowed);
            }

            if (request == null || string.IsNullOrEmpty(request.MethodName))
                return (WitResponse.BadRequest("Request is empty"), HttpStatusCode.BadRequest);

            request.Token = token ?? request.Token;

            var response = await ProcessWithTimeoutAsync(request).ConfigureAwait(false);
            return (response, ToHttpStatus(response.Status));
        }

        private async Task<WitResponse> ProcessWithTimeoutAsync(WitRequest request)
        {
            var task = RequestProcessor.Process(request);

            if (Timeout == null || Timeout == TimeSpan.Zero)
                return await task.ConfigureAwait(false);

            try
            {
                return await task.WaitAsync(Timeout.Value).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return WitResponse.InternalServerError("Request processing timed out");
            }
        }

        #endregion

        #region Tools

        private async Task<byte[]?> ReadBodyAsync(HttpListenerRequest httpRequest)
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];

            int read;
            while ((read = await httpRequest.InputStream.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false)) > 0)
            {
                buffer.Write(chunk, 0, read);

                if (buffer.Length > MaxBodyBytes)
                    return null;
            }

            return buffer.ToArray();
        }

        private static string? GetBearerToken(HttpListenerRequest httpRequest)
        {
            var header = httpRequest.Headers["Authorization"];
            if (string.IsNullOrEmpty(header))
                return null;

            if (AuthenticationHeaderValue.TryParse(header, out var value) && value.Scheme == "Bearer")
                return value.Parameter;

            return null;
        }

        private static string MethodFromUrl(HttpListenerRequest httpRequest)
        {
            var segments = httpRequest.Url?.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return segments is { Length: > 0 } ? segments[^1] : "";
        }

        private static HttpStatusCode ToHttpStatus(Model.CommunicationStatus status)
        {
            return status switch
            {
                Model.CommunicationStatus.Ok => HttpStatusCode.OK,
                Model.CommunicationStatus.BadRequest => HttpStatusCode.BadRequest,
                Model.CommunicationStatus.Unauthorized => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private void WriteResponse(HttpListenerResponse httpResponse, WitResponse response, HttpStatusCode status)
        {
            byte[] bytes = Serializer.Serialize(response);

            httpResponse.StatusCode = (int)status;
            httpResponse.ContentType = JSON_MEDIA_TYPE;
            httpResponse.ContentLength64 = bytes.Length;

            using var output = httpResponse.OutputStream;
            output.Write(bytes, 0, bytes.Length);
        }

        private void TryWriteError(HttpListenerResponse httpResponse)
        {
            try
            {
                WriteResponse(httpResponse, WitResponse.InternalServerError("Failed to process the request"), HttpStatusCode.InternalServerError);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_isDisposed)
                return;

            m_isDisposed = true;

            TokenSource?.Cancel(false);
            TokenSource?.Dispose();
            TokenSource = null;

            Listener?.Close();
            Listener = null;

            m_limit.Dispose();
        }

        #endregion

        #region Properties

        private HttpListener? Listener { get; set; }

        private CancellationTokenSource? TokenSource { get; set; }

        #endregion

        #region Services

        private IRequestProcessor RequestProcessor { get; }

        private RestServerTransportOptions Options { get; }

        private IMessageSerializer Serializer { get; }

        private IAccessTokenValidator TokenValidator { get; }

        private ILogger? Logger { get; }

        private TimeSpan? Timeout { get; }

        private long MaxBodyBytes { get; }

        #endregion
    }
}
