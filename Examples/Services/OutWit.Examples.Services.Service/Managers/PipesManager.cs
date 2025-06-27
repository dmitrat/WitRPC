using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;

namespace OutWit.Examples.Services.Service.Managers
{
    public class PipesManager
    {
        #region Constants

        private const string PIPE_NAME = "servicepipe";

        private const int MAX_CLIENTS = 100;

        #endregion

        #region Constructors

        public PipesManager(ExampleService service, ILogger<PipesManager> logger)
        {
            Service = service;
            Logger = logger;
        }

        #endregion

        #region Functions

        public void StartPipesServer()
        {
            Logger.LogInformation($"Starting Named Pipes Server, Pipe name: {PIPE_NAME}, max number of clients: {MAX_CLIENTS}");

            Server = WitServerBuilder.Build(options =>
            {
                options.WithService(Service);
                options.WithNamedPipe(PIPE_NAME, MAX_CLIENTS);
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

        private ILogger<PipesManager> Logger { get; }

        private WitServer? Server { get; set; }

        #endregion
    }
}
