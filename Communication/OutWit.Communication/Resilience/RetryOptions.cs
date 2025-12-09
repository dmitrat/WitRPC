using System;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Model;

namespace OutWit.Communication.Resilience
{
    /// <summary>
    /// Options for configuring retry behavior.
    /// </summary>
    public class RetryOptions : ModelBase
    {
        #region Constants

        private const int DEFAULT_MAX_RETRIES = 3;
        private static readonly TimeSpan DEFAULT_INITIAL_DELAY = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan DEFAULT_MAX_DELAY = TimeSpan.FromSeconds(30);
        private const double DEFAULT_BACKOFF_MULTIPLIER = 2.0;

        #endregion

        #region Constructors

        public RetryOptions()
        {
            Enabled = false;
            MaxRetries = DEFAULT_MAX_RETRIES;
            InitialDelay = DEFAULT_INITIAL_DELAY;
            MaxDelay = DEFAULT_MAX_DELAY;
            BackoffMultiplier = DEFAULT_BACKOFF_MULTIPLIER;
            BackoffType = BackoffType.Exponential;
            RetryableStatuses = new HashSet<CommunicationStatus>
            {
                CommunicationStatus.InternalServerError
            };
            RetryableExceptionTypes = new HashSet<Type>();
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Enabled: {Enabled}, MaxRetries: {MaxRetries}, BackoffType: {BackoffType}";
        }

        /// <summary>
        /// Calculates the delay for a given retry attempt.
        /// </summary>
        public TimeSpan GetDelayForAttempt(int attempt)
        {
            if (attempt <= 0)
                return InitialDelay;

            double delayMs = BackoffType switch
            {
                BackoffType.Fixed => InitialDelay.TotalMilliseconds,
                BackoffType.Linear => InitialDelay.TotalMilliseconds * attempt,
                BackoffType.Exponential => InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1),
                _ => InitialDelay.TotalMilliseconds
            };

            return TimeSpan.FromMilliseconds(Math.Min(delayMs, MaxDelay.TotalMilliseconds));
        }

        /// <summary>
        /// Determines if a retry should be attempted based on the exception.
        /// </summary>
        public bool ShouldRetry(Exception exception)
        {
            if (!Enabled)
                return false;

            if (RetryableExceptionTypes.Count == 0)
                return true;

            var exceptionType = exception.GetType();
            foreach (var retryableType in RetryableExceptionTypes)
            {
                if (retryableType.IsAssignableFrom(exceptionType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a retry should be attempted based on the response status.
        /// </summary>
        public bool ShouldRetry(CommunicationStatus status)
        {
            if (!Enabled)
                return false;

            return RetryableStatuses.Contains(status);
        }

        /// <summary>
        /// Adds an exception type that should trigger a retry.
        /// </summary>
        public RetryOptions RetryOn<TException>() where TException : Exception
        {
            RetryableExceptionTypes.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Adds a communication status that should trigger a retry.
        /// </summary>
        public RetryOptions RetryOnStatus(CommunicationStatus status)
        {
            RetryableStatuses.Add(status);
            return this;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not RetryOptions options)
                return false;

            return Enabled.Is(options.Enabled) &&
                   MaxRetries.Is(options.MaxRetries) &&
                   InitialDelay.Is(options.InitialDelay) &&
                   MaxDelay.Is(options.MaxDelay) &&
                   BackoffMultiplier.Is(options.BackoffMultiplier, tolerance) &&
                   BackoffType.Is(options.BackoffType);
        }

        public override RetryOptions Clone()
        {
            return new RetryOptions
            {
                Enabled = Enabled,
                MaxRetries = MaxRetries,
                InitialDelay = InitialDelay,
                MaxDelay = MaxDelay,
                BackoffMultiplier = BackoffMultiplier,
                BackoffType = BackoffType,
                RetryableStatuses = new HashSet<CommunicationStatus>(RetryableStatuses),
                RetryableExceptionTypes = new HashSet<Type>(RetryableExceptionTypes),
                OnRetry = OnRetry
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether retry is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the initial delay before the first retry.
        /// </summary>
        public TimeSpan InitialDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan MaxDelay { get; set; }

        /// <summary>
        /// Gets or sets the multiplier for exponential backoff.
        /// </summary>
        public double BackoffMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the backoff strategy type.
        /// </summary>
        public BackoffType BackoffType { get; set; }

        /// <summary>
        /// Gets the set of communication statuses that should trigger a retry.
        /// </summary>
        public HashSet<CommunicationStatus> RetryableStatuses { get; private set; }

        /// <summary>
        /// Gets the set of exception types that should trigger a retry.
        /// </summary>
        public HashSet<Type> RetryableExceptionTypes { get; private set; }

        #endregion

        #region Callbacks

        /// <summary>
        /// Callback invoked before each retry attempt.
        /// Parameters: exception (if any), attempt number, delay before retry.
        /// </summary>
        public Action<Exception?, int, TimeSpan>? OnRetry { get; set; }

        #endregion
    }
}
