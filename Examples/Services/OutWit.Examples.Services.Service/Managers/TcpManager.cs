using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Communication.Server.Tcp.Utils;
using OutWit.Examples.Services.Service.Utils;

namespace OutWit.Examples.Services.Service.Managers
{
    public class TcpManager
    {
        #region Constants

        private const int PORT = 5051;

        private const int MAX_CLIENTS = 255;

        #endregion

        #region Constructors

        public TcpManager(ExampleService service, ILogger<TcpManager> logger)
        {
            Service = service;
            Logger = logger;
        }

        #endregion

        #region Functions

        public void StartTcpServer()
        {
            Logger.LogInformation($"Starting TCP Server, port: {PORT}, max number of clients: {MAX_CLIENTS}");

            Server = WitComServerBuilder.Build(options =>
            {
                options.WithService(Service);
                options.WithTcp(PORT, MAX_CLIENTS);
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

        private ILogger<TcpManager> Logger { get; }

        private WitComServer? Server { get; set; }

        #endregion
    }
}
