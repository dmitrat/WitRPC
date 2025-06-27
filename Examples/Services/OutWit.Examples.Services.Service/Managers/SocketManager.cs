using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Examples.Services.Service.Utils;

namespace OutWit.Examples.Services.Service.Managers
{
    public class SocketManager
    {
        #region Constants

        private const int MAX_CLIENTS = 255;

        private const int PORT = 5052;

        #endregion

        #region Constructors

        public SocketManager(ExampleService service, ILogger<SocketManager> logger)
        {
            Service = service;
            Logger = logger;
        }

        #endregion

        #region Functions

        public void StartSocketServer()
        {
            var url = $"http://localhost:{PORT}/webSocket/";

            Logger.LogInformation($"Starting WebSocket Server, url: {url}, max number of clients: {MAX_CLIENTS}");

            Server = WitServerBuilder.Build(options =>
            {
                options.WithService(Service);
                options.WithWebSocket(url, MAX_CLIENTS);
                options.WithEncryption();
                options.WithJson();
                options.WithTimeout(TimeSpan.FromSeconds(1));
                options.WithLogger(Logger);
            });

            Server.StartWaitingForConnection();
        }

        #endregion

        #region Properties

        private ExampleService Service { get; }

        private ILogger<SocketManager> Logger { get; }

        private WitServer? Server { get; set; }

        #endregion
    }
}
