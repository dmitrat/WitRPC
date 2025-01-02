using System;

namespace OutWit.Examples.Services.Service
{
    public class Program
    {
        #region Constants

        private const string ENVIRONMENT_KEY = "--env=";
        private const string DEFAULT_ENVIRONMENT = "Development";

        #endregion

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            string environment = GetEnvironmentProfileName(args);

            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseEnvironment(environment)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                    builder.AddJsonFile("appsettings.json", true);
                    builder.AddJsonFile($"appsettings.{environment}.json", true);
                })
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseStartup<Startup>(context =>
                    {
                        context.HostingEnvironment.EnvironmentName = environment;
                        return new Startup(context.Configuration, context.HostingEnvironment);
                    });
                    builder.UseKestrel();
                });
        }

        private static string GetEnvironmentProfileName(string[] args)
        {
            string environment = DEFAULT_ENVIRONMENT;
            string? envArg = args.FirstOrDefault(a => a.StartsWith(ENVIRONMENT_KEY));
            if(string.IsNullOrEmpty(envArg))
                return environment;

            string[] parts = envArg.Split('=');
            if (parts.Length == 2)
                return parts[1];
            
            return environment;
        }
    }
}
