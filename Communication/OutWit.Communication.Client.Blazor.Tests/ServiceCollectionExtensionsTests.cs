using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OutWit.Communication.Client.Blazor.Tests.Mocks;

namespace OutWit.Communication.Client.Blazor.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        #region Helpers

        private static ServiceCollection CreateBaseServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<NavigationManager>(new TestNavigationManager());
            services.AddSingleton<IJSRuntime>(new TestJSRuntime());
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            return services;
        }

        #endregion

        #region Registration Tests

        [Test]
        public async Task AddWitRpcChannelDefaultOptionsRegistersFactoryAndTokenProviderTest()
        {
            var services = CreateBaseServices();

            services.AddWitRpcChannel();

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var factory = scope.ServiceProvider.GetService<IChannelFactory>();
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory, Is.InstanceOf<ChannelFactory>());

            var tokenProvider = scope.ServiceProvider.GetService<ChannelTokenProvider>();
            Assert.That(tokenProvider, Is.Not.Null);
        }

        [Test]
        public async Task AddWitRpcChannelWithoutBlazorAuthResolvesSuccessfullyTest()
        {
            var services = CreateBaseServices();

            services.AddWitRpcChannel();

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var factory = scope.ServiceProvider.GetService<IChannelFactory>();
            Assert.That(factory, Is.Not.Null);

            var tokenProvider = scope.ServiceProvider.GetService<ChannelTokenProvider>();
            Assert.That(tokenProvider, Is.Not.Null);
        }

        [Test]
        public async Task AddWitRpcChannelWithBlazorAuthResolvesSuccessfullyTest()
        {
            var services = CreateBaseServices();
            services.AddSingleton<IAccessTokenProvider>(new TestAccessTokenProvider("tok"));

            services.AddWitRpcChannel();

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var factory = scope.ServiceProvider.GetService<IChannelFactory>();
            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public async Task AddWitRpcChannelCalledTwiceNoDuplicateRegistrationsTest()
        {
            var services = CreateBaseServices();

            services.AddWitRpcChannel();
            services.AddWitRpcChannel(opts => opts.ApiPath = "second");

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var options = provider.GetService<ChannelFactoryOptions>();
            Assert.That(options, Is.Not.Null);
            Assert.That(options!.ApiPath, Is.EqualTo("api"));

            var factories = scope.ServiceProvider.GetServices<IChannelFactory>();
            Assert.That(factories.Count(), Is.EqualTo(1));
        }

        #endregion

        #region Options Tests

        [Test]
        public void AddWitRpcChannelCustomOptionsAreAppliedTest()
        {
            var services = CreateBaseServices();

            services.AddWitRpcChannel(opts =>
            {
                opts.ApiPath = "custom-api";
                opts.TimeoutSeconds = 30;
                opts.UseEncryption = false;
                opts.Reconnect = null;
                opts.Retry = null;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetService<ChannelFactoryOptions>();

            Assert.That(options, Is.Not.Null);
            Assert.That(options!.ApiPath, Is.EqualTo("custom-api"));
            Assert.That(options.TimeoutSeconds, Is.EqualTo(30));
            Assert.That(options.UseEncryption, Is.False);
            Assert.That(options.Reconnect, Is.Null);
            Assert.That(options.Retry, Is.Null);
        }

        [Test]
        public async Task AddWitRpcChannelMinimalConfigResolvesOkTest()
        {
            var services = CreateBaseServices();

            services.AddWitRpcChannel(opts =>
            {
                opts.UseEncryption = false;
                opts.Reconnect = null;
                opts.Retry = null;
            });

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var factory = scope.ServiceProvider.GetService<IChannelFactory>();
            Assert.That(factory, Is.Not.Null);

            var options = provider.GetService<ChannelFactoryOptions>();
            Assert.That(options, Is.Not.Null);
            Assert.That(options!.UseEncryption, Is.False);
            Assert.That(options.Reconnect, Is.Null);
            Assert.That(options.Retry, Is.Null);
        }

        [Test]
        public async Task AddWitRpcChannelFullConfigResolvesOkTest()
        {
            var services = CreateBaseServices();
            services.AddSingleton<IAccessTokenProvider>(new TestAccessTokenProvider("secret"));

            services.AddWitRpcChannel(opts =>
            {
                opts.ApiPath = "ws-api";
                opts.TimeoutSeconds = 20;
                opts.UseEncryption = true;
                opts.Reconnect = new ChannelReconnectOptions
                {
                    MaxAttempts = 5,
                    InitialDelay = TimeSpan.FromSeconds(2),
                    ReconnectOnDisconnect = true
                };
                opts.Retry = new ChannelRetryOptions
                {
                    MaxRetries = 5,
                    InitialDelay = TimeSpan.FromSeconds(1)
                };
            });

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();

            var factory = scope.ServiceProvider.GetService<IChannelFactory>();
            Assert.That(factory, Is.Not.Null);

            var options = provider.GetService<ChannelFactoryOptions>();
            Assert.That(options, Is.Not.Null);
            Assert.That(options!.UseEncryption, Is.True);
            Assert.That(options.Reconnect!.MaxAttempts, Is.EqualTo(5));
            Assert.That(options.Retry!.MaxRetries, Is.EqualTo(5));
        }

        #endregion
    }
}
