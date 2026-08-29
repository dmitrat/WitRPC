using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Server.Rest
{
    /// <summary>
    /// The REST transport server -- WitRPC's compatibility layer for callers
    /// that are not WitRPC at all. One HTTP endpoint per method:
    /// <c>POST {base}/{method}</c> with a plain JSON body (an object of named
    /// arguments or an array of positional ones), or <c>GET {base}/{method}?a=1</c>
    /// for simple arguments. The reply is the method's return value as plain
    /// JSON (<c>204</c> for nothing), or an HTTP error status with a small JSON
    /// error object. Arguments are bound against the contract's declared
    /// parameter types, so a caller needs the method name and the values --
    /// nothing WitRPC-specific.
    /// <para>
    /// Requests are handled concurrently and independently: a slow or failing
    /// one neither blocks the accept loop nor brings the listener down. The body
    /// is size-capped and processing is time-bounded. REST is stateless
    /// request/reply, so it carries no server-to-client callbacks.
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
            RestMethodCatalog catalog, ILogger? logger, TimeSpan? timeout,
            long maxBodyBytes = DEFAULT_MAX_BODY_BYTES, int maxConcurrentRequests = int.MaxValue)
        {
            Options = options;
            Serializer = new MessageSerializerJson();
            TokenValidator = tokenValidator;
            RequestProcessor = requestProcessor;
            Catalog = catalog;

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
                    await ProcessAsync(context).ConfigureAwait(false);
                }
                finally
                {
                    m_limit.Release();
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Failed to handle a REST request");
                TryWriteError(context.Response, HttpStatusCode.InternalServerError, CommunicationStatus.InternalServerError, "Failed to process the request", null);
            }
        }

        private async Task ProcessAsync(HttpListenerContext context)
        {
            var httpRequest = context.Request;
            var httpResponse = context.Response;

            string? token = GetBearerToken(httpRequest);

            if (!TokenValidator.IsRequestTokenValid(token ?? ""))
            {
                WriteError(httpResponse, HttpStatusCode.Unauthorized, CommunicationStatus.Unauthorized, "Token is not valid", null);
                return;
            }

            string methodName = MethodFromUrl(httpRequest);
            if (string.IsNullOrEmpty(methodName))
            {
                WriteError(httpResponse, HttpStatusCode.BadRequest, CommunicationStatus.BadRequest, "The URL must end with the method name", null);
                return;
            }

            RestBinding binding;

            if (httpRequest.HttpMethod == HttpMethod.Post.Method)
            {
                if (httpRequest.ContentLength64 > MaxBodyBytes)
                {
                    WriteError(httpResponse, HttpStatusCode.RequestEntityTooLarge, CommunicationStatus.BadRequest, $"Request body exceeds the {MaxBodyBytes} byte limit", null);
                    return;
                }

                byte[]? body = await ReadBodyAsync(httpRequest).ConfigureAwait(false);
                if (body == null)
                {
                    WriteError(httpResponse, HttpStatusCode.RequestEntityTooLarge, CommunicationStatus.BadRequest, $"Request body exceeds the {MaxBodyBytes} byte limit", null);
                    return;
                }

                if (body.Length == 0)
                {
                    binding = Catalog.BindBody(methodName, null);
                }
                else
                {
                    try
                    {
                        using var document = JsonDocument.Parse(body);
                        binding = Catalog.BindBody(methodName, document.RootElement);
                    }
                    catch (JsonException e)
                    {
                        WriteError(httpResponse, HttpStatusCode.BadRequest, CommunicationStatus.BadRequest, "The body is not valid JSON", e.Message);
                        return;
                    }
                }
            }
            else if (httpRequest.HttpMethod == HttpMethod.Get.Method)
            {
                binding = Catalog.BindQuery(methodName, httpRequest.QueryString);
            }
            else
            {
                WriteError(httpResponse, HttpStatusCode.MethodNotAllowed, CommunicationStatus.BadRequest, $"Unsupported HTTP method {httpRequest.HttpMethod}", null);
                return;
            }

            if (binding.Request == null)
            {
                WriteError(httpResponse, binding.Status, CommunicationStatus.BadRequest, binding.Error ?? "Cannot bind the request", null);
                return;
            }

            binding.Request.Token = token ?? "";

            var response = await ProcessWithTimeoutAsync(binding.Request).ConfigureAwait(false);

            if (response.Status == CommunicationStatus.Ok)
                WriteResult(httpResponse, response.Data);
            else
                WriteError(httpResponse, ToHttpStatus(response.Status), response.Status, response.ErrorMessage ?? "Request failed", response.ErrorDetails);
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

        private static HttpStatusCode ToHttpStatus(CommunicationStatus status)
        {
            return status switch
            {
                CommunicationStatus.Ok => HttpStatusCode.OK,
                CommunicationStatus.BadRequest => HttpStatusCode.BadRequest,
                CommunicationStatus.Unauthorized => HttpStatusCode.Unauthorized,
                CommunicationStatus.Timeout => HttpStatusCode.RequestTimeout,
                CommunicationStatus.TransportError => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// The return value as plain JSON; nothing to return is 204 No Content.
        /// </summary>
        private static void WriteResult(HttpListenerResponse httpResponse, byte[]? data)
        {
            if (data == null || data.Length == 0)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.NoContent;
                httpResponse.Close();
                return;
            }

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            httpResponse.ContentType = JSON_MEDIA_TYPE;
            httpResponse.ContentLength64 = data.Length;

            using var output = httpResponse.OutputStream;
            output.Write(data, 0, data.Length);
        }

        /// <summary>
        /// A small, readable error object: <c>{"status":"BadRequest","error":"...","details":"..."}</c>.
        /// </summary>
        private static void WriteError(HttpListenerResponse httpResponse, HttpStatusCode httpStatus, CommunicationStatus status, string error, string? details)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("status", status.ToString());
                writer.WriteString("error", error);
                if (details != null)
                    writer.WriteString("details", details);
                writer.WriteEndObject();
            }

            byte[] bytes = stream.ToArray();

            httpResponse.StatusCode = (int)httpStatus;
            httpResponse.ContentType = JSON_MEDIA_TYPE;
            httpResponse.ContentLength64 = bytes.Length;

            using var output = httpResponse.OutputStream;
            output.Write(bytes, 0, bytes.Length);
        }

        private void TryWriteError(HttpListenerResponse httpResponse, HttpStatusCode httpStatus, CommunicationStatus status, string error, string? details)
        {
            try
            {
                WriteError(httpResponse, httpStatus, status, error, details);
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

        private RestMethodCatalog Catalog { get; }

        private RestServerTransportOptions Options { get; }

        private IMessageSerializer Serializer { get; }

        private IAccessTokenValidator TokenValidator { get; }

        private ILogger? Logger { get; }

        private TimeSpan? Timeout { get; }

        private long MaxBodyBytes { get; }

        #endregion
    }
}
