using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// Keeps every formatted log line.
    /// </summary>
    public sealed class MockLoggerCapturing : ILogger
    {
        #region Fields

        private readonly List<string> m_entries = new();

        #endregion

        #region ILogger

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (m_entries)
                m_entries.Add($"{logLevel}: {formatter(state, exception)}");
        }

        #endregion

        #region Properties

        public IReadOnlyList<string> Entries
        {
            get
            {
                lock (m_entries)
                    return m_entries.ToArray();
            }
        }

        #endregion
    }
}
