using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Communication.Client.Blazor.Tests.Mocks;

namespace OutWit.Communication.Client.Blazor.Tests
{
    [TestFixture]
    public class ChannelFactoryTests
    {
        #region Helpers

        private static ChannelFactory CreateFactory(ChannelFactoryOptions? options = null)
        {
            return new ChannelFactory(
                new TestNavigationManager(),
                authenticationProvider: null,
                new TestJSRuntime(),
                new ChannelTokenProvider(null, NullLogger<ChannelTokenProvider>.Instance),
                options ?? new ChannelFactoryOptions(),
                NullLogger<ChannelFactory>.Instance);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public async Task GetServiceAsyncWhenDisposedThrowsObjectDisposedExceptionTest()
        {
            var factory = CreateFactory();
            await factory.DisposeAsync();

            Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await factory.GetServiceAsync<IDisposable>());
        }

        [Test]
        public async Task ReconnectAsyncWhenDisposedThrowsObjectDisposedExceptionTest()
        {
            var factory = CreateFactory();
            await factory.DisposeAsync();

            Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await factory.ReconnectAsync());
        }

        [Test]
        public async Task DisposeAsyncMultipleTimesDoesNotThrowTest()
        {
            var factory = CreateFactory();

            await factory.DisposeAsync();
            await factory.DisposeAsync();
            await factory.DisposeAsync();

            Assert.Pass();
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void ConstructorWithDefaultOptionsDoesNotThrowTest()
        {
            var factory = CreateFactory();

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithEncryptionDisabledDoesNotThrowTest()
        {
            var options = new ChannelFactoryOptions { UseEncryption = false };
            var factory = CreateFactory(options);

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithAllFeaturesDisabledDoesNotThrowTest()
        {
            var options = new ChannelFactoryOptions
            {
                UseEncryption = false,
                Reconnect = null,
                Retry = null
            };

            var factory = CreateFactory(options);

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithNullAuthProviderDoesNotThrowTest()
        {
            var factory = new ChannelFactory(
                new TestNavigationManager(),
                authenticationProvider: null,
                new TestJSRuntime(),
                new ChannelTokenProvider(null, NullLogger<ChannelTokenProvider>.Instance),
                new ChannelFactoryOptions(),
                NullLogger<ChannelFactory>.Instance);

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithCustomApiPathDoesNotThrowTest()
        {
            var options = new ChannelFactoryOptions { ApiPath = "my/custom/path" };
            var factory = CreateFactory(options);

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithBaseUrlDoesNotThrowTest()
        {
            var options = new ChannelFactoryOptions { BaseUrl = "https://api.example.com" };
            var factory = CreateFactory(options);

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public void ConstructorWithBaseUrlAndCustomPathDoesNotThrowTest()
        {
            var options = new ChannelFactoryOptions
            {
                BaseUrl = "https://api.example.com",
                ApiPath = "rpc"
            };
            var factory = CreateFactory(options);

            Assert.That(factory, Is.Not.Null);
        }

        #endregion
    }
}
