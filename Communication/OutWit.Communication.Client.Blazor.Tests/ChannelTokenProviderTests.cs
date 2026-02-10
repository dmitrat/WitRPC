using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Communication.Client.Blazor.Tests.Mocks;

namespace OutWit.Communication.Client.Blazor.Tests
{
    [TestFixture]
    public class ChannelTokenProviderTests
    {
        #region GetToken Tests

        [Test]
        public async Task GetTokenWhenNoBlazorProviderReturnsEmptyTest()
        {
            var provider = new ChannelTokenProvider(
                null,
                NullLogger<ChannelTokenProvider>.Instance);

            var token = await provider.GetToken();

            Assert.That(token, Is.EqualTo(""));
        }

        [Test]
        public async Task GetTokenWhenProviderReturnsTokenReturnsValueTest()
        {
            var blazorProvider = new TestAccessTokenProvider("my-secret-token");
            var provider = new ChannelTokenProvider(
                blazorProvider,
                NullLogger<ChannelTokenProvider>.Instance);

            var token = await provider.GetToken();

            Assert.That(token, Is.EqualTo("my-secret-token"));
        }

        [Test]
        public async Task GetTokenWhenProviderFailsReturnsEmptyTest()
        {
            var blazorProvider = new TestAccessTokenProvider(null);
            var provider = new ChannelTokenProvider(
                blazorProvider,
                NullLogger<ChannelTokenProvider>.Instance);

            var token = await provider.GetToken();

            Assert.That(token, Is.EqualTo(""));
        }

        #endregion
    }
}
