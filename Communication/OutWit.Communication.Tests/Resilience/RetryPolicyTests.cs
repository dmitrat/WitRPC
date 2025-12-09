using System;
using OutWit.Communication.Model;
using OutWit.Communication.Resilience;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Tests.Resilience
{
    [TestFixture]
    public class RetryPolicyTests
    {
        #region RetryOptions Tests

        [Test]
        public void RetryOptions_DefaultValues()
        {
            var options = new RetryOptions();

            Assert.That(options.Enabled, Is.False);
            Assert.That(options.MaxRetries, Is.EqualTo(3));
            Assert.That(options.InitialDelay, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
            Assert.That(options.MaxDelay, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(options.BackoffMultiplier, Is.EqualTo(2.0));
            Assert.That(options.BackoffType, Is.EqualTo(BackoffType.Exponential));
        }

        [Test]
        public void RetryOptions_GetDelayForAttempt_FixedBackoff()
        {
            var options = new RetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffType = BackoffType.Fixed
            };

            Assert.That(options.GetDelayForAttempt(1), Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(options.GetDelayForAttempt(2), Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(options.GetDelayForAttempt(3), Is.EqualTo(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void RetryOptions_GetDelayForAttempt_LinearBackoff()
        {
            var options = new RetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffType = BackoffType.Linear,
                MaxDelay = TimeSpan.FromMinutes(1)
            };

            Assert.That(options.GetDelayForAttempt(1), Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(options.GetDelayForAttempt(2), Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(options.GetDelayForAttempt(3), Is.EqualTo(TimeSpan.FromSeconds(3)));
        }

        [Test]
        public void RetryOptions_GetDelayForAttempt_ExponentialBackoff()
        {
            var options = new RetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(1),
                BackoffMultiplier = 2.0,
                BackoffType = BackoffType.Exponential,
                MaxDelay = TimeSpan.FromMinutes(1)
            };

            Assert.That(options.GetDelayForAttempt(1), Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(options.GetDelayForAttempt(2), Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(options.GetDelayForAttempt(3), Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(options.GetDelayForAttempt(4), Is.EqualTo(TimeSpan.FromSeconds(8)));
        }

        [Test]
        public void RetryOptions_GetDelayForAttempt_CappedAtMaxDelay()
        {
            var options = new RetryOptions
            {
                InitialDelay = TimeSpan.FromSeconds(10),
                BackoffMultiplier = 2.0,
                BackoffType = BackoffType.Exponential,
                MaxDelay = TimeSpan.FromSeconds(20)
            };

            // 10 * 2^2 = 40, but capped at 20
            Assert.That(options.GetDelayForAttempt(3), Is.EqualTo(TimeSpan.FromSeconds(20)));
        }

        [Test]
        public void RetryOptions_ShouldRetry_Status()
        {
            var options = new RetryOptions { Enabled = true };
            
            options.RetryOnStatus(CommunicationStatus.InternalServerError);

            Assert.That(options.ShouldRetry(CommunicationStatus.InternalServerError), Is.True);
            Assert.That(options.ShouldRetry(CommunicationStatus.Ok), Is.False);
            Assert.That(options.ShouldRetry(CommunicationStatus.BadRequest), Is.False);
        }

        [Test]
        public void RetryOptions_ShouldRetry_Exception()
        {
            var options = new RetryOptions { Enabled = true };
            
            options.RetryOn<InvalidOperationException>();

            Assert.That(options.ShouldRetry(new InvalidOperationException()), Is.True);
            Assert.That(options.ShouldRetry(new ArgumentException()), Is.False);
        }

        [Test]
        public void RetryOptions_ShouldRetry_DisabledReturnsFalse()
        {
            var options = new RetryOptions { Enabled = false };

            Assert.That(options.ShouldRetry(CommunicationStatus.InternalServerError), Is.False);
            Assert.That(options.ShouldRetry(new Exception()), Is.False);
        }

        [Test]
        public void RetryOptions_Clone()
        {
            var original = new RetryOptions
            {
                Enabled = true,
                MaxRetries = 5,
                InitialDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                BackoffMultiplier = 1.5,
                BackoffType = BackoffType.Linear
            };

            var clone = original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Is(original), Is.True);
        }

        #endregion

        #region RetryPolicy Tests

        [Test]
        public async Task RetryPolicy_ExecuteAsync_SuccessOnFirstAttempt()
        {
            var options = new RetryOptions { Enabled = true, MaxRetries = 3 };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                return "success";
            });

            Assert.That(result, Is.EqualTo("success"));
            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task RetryPolicy_ExecuteAsync_SuccessAfterRetries()
        {
            var options = new RetryOptions 
            { 
                Enabled = true, 
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                BackoffType = BackoffType.Fixed
            };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync<string>(async () =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("Transient error");
                return "success";
            });

            Assert.That(result, Is.EqualTo("success"));
            Assert.That(attempts, Is.EqualTo(3));
        }

        [Test]
        public void RetryPolicy_ExecuteAsync_ThrowsAfterMaxRetries()
        {
            var options = new RetryOptions 
            { 
                Enabled = true, 
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                BackoffType = BackoffType.Fixed
            };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await policy.ExecuteAsync<string>(async () =>
                {
                    attempts++;
                    throw new InvalidOperationException("Persistent error");
                });
            });

            Assert.That(attempts, Is.EqualTo(3)); // Initial + 2 retries
        }

        [Test]
        public async Task RetryPolicy_ExecuteAsync_CallsOnRetryCallback()
        {
            var retryAttempts = new List<int>();
            var options = new RetryOptions 
            { 
                Enabled = true, 
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                BackoffType = BackoffType.Fixed,
                OnRetry = (ex, attempt, delay) => retryAttempts.Add(attempt)
            };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync<string>(async () =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("Transient error");
                return "success";
            });

            Assert.That(retryAttempts, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task RetryPolicy_ExecuteAsync_WitResponse_SuccessOnFirstAttempt()
        {
            var options = new RetryOptions { Enabled = true, MaxRetries = 3 };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                return WitResponse.Success(null);
            });

            Assert.That(result.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public async Task RetryPolicy_ExecuteAsync_WitResponse_RetryOnServerError()
        {
            var options = new RetryOptions 
            { 
                Enabled = true, 
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                BackoffType = BackoffType.Fixed
            };
            options.RetryOnStatus(CommunicationStatus.InternalServerError);
            
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                if (attempts < 3)
                    return WitResponse.InternalServerError("Transient error");
                return WitResponse.Success(null);
            });

            Assert.That(result.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(attempts, Is.EqualTo(3));
        }

        [Test]
        public async Task RetryPolicy_ExecuteAsync_WitResponse_NoRetryOnBadRequest()
        {
            var options = new RetryOptions 
            { 
                Enabled = true, 
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(10)
            };
            // Only retry on InternalServerError by default
            
            var policy = new RetryPolicy(options);
            var attempts = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                return WitResponse.BadRequest("Invalid request");
            });

            Assert.That(result.Status, Is.EqualTo(CommunicationStatus.BadRequest));
            Assert.That(attempts, Is.EqualTo(1)); // No retry for BadRequest
        }

        [Test]
        public async Task RetryPolicy_Disabled_NoRetry()
        {
            var options = new RetryOptions { Enabled = false, MaxRetries = 3 };
            var policy = new RetryPolicy(options);
            var attempts = 0;

            try
            {
                await policy.ExecuteAsync<string>(async () =>
                {
                    attempts++;
                    throw new InvalidOperationException("Error");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(attempts, Is.EqualTo(1)); // No retry when disabled
        }

        #endregion
    }
}
