namespace OutWit.Communication.Client.Blazor.Tests
{
    [TestFixture]
    public class ChannelFactoryOptionsTests
    {
        #region Default Values Tests

        [Test]
        public void DefaultValuesAreCorrectTest()
        {
            var options = new ChannelFactoryOptions();

            Assert.That(options.ApiPath, Is.EqualTo("api"));
            Assert.That(options.TimeoutSeconds, Is.EqualTo(10));
            Assert.That(options.UseEncryption, Is.True);
            Assert.That(options.Reconnect, Is.Not.Null);
            Assert.That(options.Retry, Is.Not.Null);
            Assert.That(options.ConfigureClient, Is.Null);
        }

        [Test]
        public void ReconnectDefaultsAreCorrectTest()
        {
            var reconnect = new ChannelReconnectOptions();

            Assert.That(reconnect.MaxAttempts, Is.EqualTo(0));
            Assert.That(reconnect.InitialDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(reconnect.MaxDelay, Is.EqualTo(TimeSpan.FromMinutes(2)));
            Assert.That(reconnect.BackoffMultiplier, Is.EqualTo(2.0));
            Assert.That(reconnect.ReconnectOnDisconnect, Is.True);
        }

        [Test]
        public void RetryDefaultsAreCorrectTest()
        {
            var retry = new ChannelRetryOptions();

            Assert.That(retry.MaxRetries, Is.EqualTo(3));
            Assert.That(retry.InitialDelay, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
            Assert.That(retry.MaxDelay, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(retry.BackoffMultiplier, Is.EqualTo(2.0));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void AllFeaturesDisabledIsValidTest()
        {
            var options = new ChannelFactoryOptions
            {
                UseEncryption = false,
                Reconnect = null,
                Retry = null,
                ConfigureClient = null
            };

            Assert.That(options.UseEncryption, Is.False);
            Assert.That(options.Reconnect, Is.Null);
            Assert.That(options.Retry, Is.Null);
        }

        [Test]
        public void ConfigureClientCanBeSetTest()
        {
            var called = false;
            var options = new ChannelFactoryOptions
            {
                ConfigureClient = _ => called = true
            };

            Assert.That(options.ConfigureClient, Is.Not.Null);
            options.ConfigureClient!(null!);
            Assert.That(called, Is.True);
        }

        #endregion
    }
}
