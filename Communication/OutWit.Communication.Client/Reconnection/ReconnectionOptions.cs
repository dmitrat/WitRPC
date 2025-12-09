using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Client.Reconnection
{
    /// <summary>
    /// Options for automatic reconnection behavior.
    /// </summary>
    public class ReconnectionOptions : ModelBase
    {
        #region Constants

        private const int DEFAULT_MAX_ATTEMPTS = 10;
        private static readonly TimeSpan DEFAULT_INITIAL_DELAY = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DEFAULT_MAX_DELAY = TimeSpan.FromMinutes(2);
        private const double DEFAULT_BACKOFF_MULTIPLIER = 2.0;

        #endregion

        #region Constructors

        public ReconnectionOptions()
        {
            Enabled = false;
            MaxAttempts = DEFAULT_MAX_ATTEMPTS;
            InitialDelay = DEFAULT_INITIAL_DELAY;
            MaxDelay = DEFAULT_MAX_DELAY;
            BackoffMultiplier = DEFAULT_BACKOFF_MULTIPLIER;
            ReconnectOnDisconnect = true;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Enabled: {Enabled}, MaxAttempts: {MaxAttempts}, InitialDelay: {InitialDelay}";
        }

        /// <summary>
        /// Calculates the delay for a given attempt number using exponential backoff.
        /// </summary>
        public TimeSpan GetDelayForAttempt(int attempt)
        {
            if (attempt <= 0)
                return InitialDelay;

            var delay = InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1);
            var cappedDelay = Math.Min(delay, MaxDelay.TotalMilliseconds);
            
            return TimeSpan.FromMilliseconds(cappedDelay);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not ReconnectionOptions options)
                return false;

            return Enabled.Is(options.Enabled) &&
                   MaxAttempts.Is(options.MaxAttempts) &&
                   InitialDelay.Is(options.InitialDelay) &&
                   MaxDelay.Is(options.MaxDelay) &&
                   BackoffMultiplier.Is(options.BackoffMultiplier, tolerance) &&
                   ReconnectOnDisconnect.Is(options.ReconnectOnDisconnect);
        }

        public override ReconnectionOptions Clone()
        {
            return new ReconnectionOptions
            {
                Enabled = Enabled,
                MaxAttempts = MaxAttempts,
                InitialDelay = InitialDelay,
                MaxDelay = MaxDelay,
                BackoffMultiplier = BackoffMultiplier,
                ReconnectOnDisconnect = ReconnectOnDisconnect,
                OnReconnecting = OnReconnecting,
                OnReconnected = OnReconnected,
                OnReconnectionFailed = OnReconnectionFailed
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether automatic reconnection is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of reconnection attempts.
        /// Set to 0 for unlimited attempts.
        /// </summary>
        public int MaxAttempts { get; set; }

        /// <summary>
        /// Gets or sets the initial delay before the first reconnection attempt.
        /// </summary>
        public TimeSpan InitialDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay between reconnection attempts.
        /// </summary>
        public TimeSpan MaxDelay { get; set; }

        /// <summary>
        /// Gets or sets the multiplier for exponential backoff.
        /// </summary>
        public double BackoffMultiplier { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically reconnect when disconnected by the server.
        /// </summary>
        public bool ReconnectOnDisconnect { get; set; }

        #endregion

        #region Callbacks

        /// <summary>
        /// Callback invoked when a reconnection attempt starts.
        /// Parameters: attempt number, delay before attempt.
        /// </summary>
        public Action<int, TimeSpan>? OnReconnecting { get; set; }

        /// <summary>
        /// Callback invoked when reconnection succeeds.
        /// </summary>
        public Action? OnReconnected { get; set; }

        /// <summary>
        /// Callback invoked when all reconnection attempts have failed.
        /// Parameter: last exception.
        /// </summary>
        public Action<Exception?>? OnReconnectionFailed { get; set; }

        #endregion
    }
}
