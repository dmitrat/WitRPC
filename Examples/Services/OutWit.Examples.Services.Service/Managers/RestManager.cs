using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Server.WebSocket.Utils;
using OutWit.Examples.Services.Service.Utils;

namespace OutWit.Examples.Services.Service.Managers
{
    public class RestManager
    {
        #region Constants

        private const int PORT = 5053;

        #endregion

        #region Constructors

        public RestManager(ExampleService service, ILogger<RestManager> logger)
        {
            Service = service;
            Logger = logger;
        }

        #endregion

        #region Functions

        public void StartRestServer()
        {
            var url = $"http://localhost:{PORT}/rest/";

            Logger.LogInformation($"Starting Rest Server, url: {url}");

            Server = WitComServerRestBuilder.Build(options =>
            {
                options.WithService(Service);
                options.WithUrl(url);
                options.WithTimeout(TimeSpan.FromSeconds(1));
                options.WithLogger(Logger);
            });

            Server.StartWaitingForConnection();
        }

        #endregion

        #region Properties

        private ExampleService Service { get; }

        private ILogger<RestManager> Logger { get; }

        private WitComServerRest? Server { get; set; }

        #endregion
    }
}
