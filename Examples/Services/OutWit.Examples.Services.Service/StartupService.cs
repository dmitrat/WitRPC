using OutWit.Examples.Services.Service.Managers;

namespace OutWit.Examples.Services.Service
{
    public class StartupService : BackgroundService
    {
        #region Constructors

        public StartupService(PipesManager pipesManager, TcpManager tcpManager, SocketManager webSocketManager, RestManager restManager)
        {
            PipesManager = pipesManager;
            TcpManager = tcpManager;
            SocketManager = webSocketManager;
            RestManager = restManager;
        }

        #endregion

        #region Functions

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            PipesManager.StartPipesServer();
            TcpManager.StartTcpServer();
            SocketManager.StartSocketServer();
            RestManager.StartRestServer();
        }

        #endregion

        #region Properties

        private PipesManager PipesManager { get; }

        private TcpManager TcpManager { get; }

        private SocketManager SocketManager { get; }

        private RestManager RestManager { get; }

        #endregion
    }
}
