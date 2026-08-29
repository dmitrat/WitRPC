using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client.Rest
{
    /// <summary>
    /// The REST transport client. Each call is one stateless HTTP request in the
    /// same readable shape any other client would send: <c>POST {base}/{method}</c>
    /// with a JSON array of the arguments (or <c>GET {base}/{method}?param1=...</c>
    /// when the mode allows one), <c>Authorization: Bearer</c> from the token
    /// provider, and the return value read back as plain JSON -- or, on an HTTP
    /// error status, the server's JSON error object turned into a fault.
    /// </summary>
    public class WitClientRest : IClient
    {
        #region Constants

        private const string JSON_MEDIA_TYPE = "application/json";

        private const string POSITIONAL_PREFIX = "param";

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
            TokenProvider = tokenProvider;
        }

        #endregion

        #region IClient

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            if (request == null)
                return WitResponse.BadRequest("Empty request");

            if (request.GenericArguments.Length > 0 || request.GenericArgumentsByName.Length > 0)
                return WitResponse.BadRequest("Generic methods cannot be called over REST");

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

                return httpResponse.IsSuccessStatusCode
                    ? WitResponse.Success(body)
                    : ToFault(httpResponse.StatusCode, body);
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
            var host = Options.Host!.AppendPath(request.MethodName);

            HttpRequestMessage message;
            if (UseGet(request))
            {
                var builder = new UriBuilder(host.BuildConnection(true)) { Query = BuildQuery(request) };
                message = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            }
            else
            {
                message = new HttpRequestMessage(HttpMethod.Post, new Uri(host.BuildConnection(true)))
                {
                    Content = new ByteArrayContent(BuildBody(request))
                };
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(JSON_MEDIA_TYPE);
            }

            if (!string.IsNullOrEmpty(request.Token))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);

            return message;
        }

        private bool UseGet(WitRequest request)
        {
            int count = request.Parameters.Length;

            return Options.Mode switch
            {
                RestClientRequestModes.AllowGet => true,
                RestClientRequestModes.AllowGetForMethodsWithSingleParameter => count <= 1,
                RestClientRequestModes.AllowGetForMethodsWithoutParameters => count == 0,
                _ => false
            };
        }

        /// <summary>
        /// The arguments as one JSON array, each already serialized by the JSON
        /// parameters serializer; a null argument is JSON null.
        /// </summary>
        private static byte[] BuildBody(WitRequest request)
        {
            var body = new StringBuilder("[");

            for (int i = 0; i < request.Parameters.Length; i++)
            {
                if (i > 0)
                    body.Append(',');

                var parameter = request.Parameters[i];
                body.Append(parameter == null || parameter.Length == 0 ? "null" : Encoding.UTF8.GetString(parameter));
            }

            body.Append(']');
            return Encoding.UTF8.GetBytes(body.ToString());
        }

        /// <summary>
        /// <c>param1=...&amp;param2=...</c>; a JSON string travels unquoted, anything
        /// else as its JSON text, so the query stays readable and the server binds
        /// it against the declared types.
        /// </summary>
        private static string BuildQuery(WitRequest request)
        {
            var parts = new List<string>();

            for (int i = 0; i < request.Parameters.Length; i++)
            {
                var parameter = request.Parameters[i];
                string text = parameter == null || parameter.Length == 0 ? "null" : Encoding.UTF8.GetString(parameter);

                if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
                    text = JsonSerializer.Deserialize<string>(text) ?? "";

                parts.Add($"{POSITIONAL_PREFIX}{i + 1}={Uri.EscapeDataString(text)}");
            }

            return string.Join("&", parts);
        }

        /// <summary>
        /// Turns the server's error object (<c>{"status":..,"error":..,"details":..}</c>)
        /// into the fault the proxy throws; an unreadable body falls back to the
        /// HTTP status alone.
        /// </summary>
        private static WitResponse ToFault(HttpStatusCode httpStatus, byte[] body)
        {
            string? status = null;
            string error = $"HTTP {(int)httpStatus}";
            string? details = null;

            if (body.Length > 0)
            {
                try
                {
                    using var document = JsonDocument.Parse(body);
                    var root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("status", out var statusElement))
                            status = statusElement.GetString();
                        if (root.TryGetProperty("error", out var errorElement))
                            error = errorElement.GetString() ?? error;
                        if (root.TryGetProperty("details", out var detailsElement))
                            details = detailsElement.GetString();
                    }
                }
                catch (JsonException)
                {
                }
            }

            string message = details == null ? error : $"{error}: {details}";

            if (status == nameof(CommunicationStatus.Unauthorized) || httpStatus == HttpStatusCode.Unauthorized)
                return WitResponse.UnauthorizedRequest(message);

            if (status == nameof(CommunicationStatus.Timeout) || httpStatus == HttpStatusCode.RequestTimeout)
                return WitResponse.Timeout(message);

            if (status == nameof(CommunicationStatus.TransportError) || httpStatus == HttpStatusCode.BadGateway)
                return WitResponse.TransportError(message);

            if (status == nameof(CommunicationStatus.BadRequest) || (int)httpStatus >= 400 && (int)httpStatus < 500)
                return WitResponse.BadRequest(message);

            return WitResponse.InternalServerError(message);
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

        public IAccessTokenProvider TokenProvider { get; }

        #endregion
    }
}
