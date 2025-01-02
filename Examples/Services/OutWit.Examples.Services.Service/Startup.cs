using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OutWit.Common.Random;
using OutWit.Communication.Server;
using OutWit.Communication.Server.Pipes.Utils;
using OutWit.Examples.Contracts;
using OutWit.Examples.Services.Service.Managers;

namespace OutWit.Examples.Services.Service
{
    public class Startup
    {
        #region Constructors

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        #endregion

        #region Configuration

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<StartupService>();

            services.AddSingleton<ExampleService>();

            services.AddSingleton<PipesManager>();
            services.AddSingleton<TcpManager>();
            services.AddSingleton<SocketManager>();
            services.AddSingleton<RestManager>();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        #endregion

        #region Properties

        private IConfiguration Configuration { get; }

        private IWebHostEnvironment Environment { get; }

        #endregion
    }
}
