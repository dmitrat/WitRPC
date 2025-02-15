using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Converters;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;
using OutWit.Communication.Server.Rest.Exceptions;
using OutWit.Communication.Server.Rest.Utils;

namespace OutWit.Communication.Server.Rest
{
    public class WitComServerRest : IDisposable
    {
        #region Constants

        private const string JSON_MEDIA_TYPE = "application/json";

        #endregion
        #region Constructors

        public WitComServerRest(RestServerTransportOptions options, IAccessTokenValidator tokenValidator, IRequestProcessor requestProcessor, 
            ILogger? logger, TimeSpan? timeout)
        {
            Options = options;
            Serializer = new MessageSerializerJson();
            TokenValidator = tokenValidator;
            RequestProcessor = requestProcessor;

            Logger = logger;
            Timeout = timeout;
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection()
        {
            if(Listener != null)
                return;

            Listener = new HttpListener();
            Listener.Prefixes.Add(Options.Host!.BuildConnection());
            TokenSource = new CancellationTokenSource();

            Listener.Start();

            Task.Run(async () =>
            {
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    var context = await Listener.GetContextAsync();
                    if (TokenSource.IsCancellationRequested)
                        return;

                    await ProcessRequest(context);
                }
            });
        }

        public void StopWaitingForConnection()
        {
            Dispose();
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            var httpRequest = context.Request;

            WitComRequest? request = null;

            try
            {
                request = httpRequest.RestoreFromGet(TokenValidator)
                          ?? httpRequest.RestoreFromPost(TokenValidator);
            }
            catch (WitComExceptionRest e)
            {
                Logger?.LogError(e, "Failed to process request");
                SendResponse(context.Response, WitComResponse.BadRequest("Failed to process request", e));
                return;
            }

            SendResponse(context.Response,  await RequestProcessor.Process(request));
        }

        private void SendResponse(HttpListenerResponse httpResponse, WitComResponse response)
        {
            httpResponse.StatusCode = (int)response.Status;
            httpResponse.ContentType = JSON_MEDIA_TYPE;

            var bytes = Serializer.Serialize(response);
            httpResponse.ContentLength64 = bytes.Length;

            using var output = httpResponse.OutputStream;
            output.Write(bytes, 0, bytes.Length);
        }

        #endregion


        public void Dispose()
        {
            TokenSource?.Cancel(false);
            TokenSource?.Dispose();
            TokenSource = null;

            Listener?.Close();
            Listener = null;
        }

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

        #endregion
    }
}
