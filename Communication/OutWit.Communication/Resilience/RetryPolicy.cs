using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Model;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Resilience
{
    /// <summary>
    /// Executes operations with retry logic based on configured options.
    /// </summary>
    public class RetryPolicy
    {
        #region Constructors

        public RetryPolicy(RetryOptions options, ILogger? logger = null)
        {
            Options = options;
            Logger = logger;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Executes an async operation with retry logic.
        /// </summary>
        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation)
        {
            if (!Options.Enabled)
                return await operation();

            Exception? lastException = null;

            for (int attempt = 1; attempt <= Options.MaxRetries + 1; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt > Options.MaxRetries || !Options.ShouldRetry(ex))
                    {
                        Logger?.LogError(ex, $"Operation failed after {attempt} attempts");
                        throw;
                    }

                    var delay = Options.GetDelayForAttempt(attempt);
                    Logger?.LogWarning(ex, $"Operation failed, retry attempt {attempt}/{Options.MaxRetries} in {delay}");

                    Options.OnRetry?.Invoke(ex, attempt, delay);

                    await Task.Delay(delay);
                }
            }

            throw lastException ?? new InvalidOperationException("Retry failed without exception");
        }

        /// <summary>
        /// Executes an async operation that returns WitResponse with retry logic.
        /// </summary>
        public async Task<WitResponse> ExecuteAsync(Func<Task<WitResponse>> operation)
        {
            if (!Options.Enabled)
                return await operation();

            WitResponse? lastResponse = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= Options.MaxRetries + 1; attempt++)
            {
                try
                {
                    var response = await operation();
                    
                    if (response.IsSuccess() || !Options.ShouldRetry(response.Status))
                        return response;

                    lastResponse = response;

                    if (attempt > Options.MaxRetries)
                    {
                        Logger?.LogError($"Operation failed with status {response.Status} after {attempt} attempts");
                        return response;
                    }

                    var delay = Options.GetDelayForAttempt(attempt);
                    Logger?.LogWarning($"Operation returned {response.Status}, retry attempt {attempt}/{Options.MaxRetries} in {delay}");

                    Options.OnRetry?.Invoke(null, attempt, delay);

                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt > Options.MaxRetries || !Options.ShouldRetry(ex))
                    {
                        Logger?.LogError(ex, $"Operation failed after {attempt} attempts");
                        throw;
                    }

                    var delay = Options.GetDelayForAttempt(attempt);
                    Logger?.LogWarning(ex, $"Operation failed, retry attempt {attempt}/{Options.MaxRetries} in {delay}");

                    Options.OnRetry?.Invoke(ex, attempt, delay);

                    await Task.Delay(delay);
                }
            }

            return lastResponse ?? WitResponse.InternalServerError("Retry exhausted", lastException);
        }

        #endregion

        #region Properties

        private RetryOptions Options { get; }

        private ILogger? Logger { get; }

        #endregion
    }
}
