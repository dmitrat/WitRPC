using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Messages;
using OutWit.Communication.Server.Callbacks;

namespace OutWit.Communication.Server.Connections
{
    /// <summary>
    /// The one ordered outbound queue of a connection, drained by one writer task. Responses,
    /// handshake replies and callbacks all go through it, so the frames of a connection leave in
    /// the order they were enqueued and the AEAD counter advances in wire order. Callbacks are
    /// counted against the server's <see cref="CallbackDeliveryOptions"/> bounds; responses never
    /// are and are never refused. A frame's completion task reports what became of it.
    /// </summary>
    internal sealed class ConnectionOutbox : IDisposable
    {
        #region Fields

        private readonly Channel<OutboundFrame> m_frames = Channel.CreateUnbounded<OutboundFrame>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        private readonly Guid m_connectionId;

        private readonly Func<WitMessage, Task> m_write;

        private readonly CallbackDeliveryOptions m_options;

        private readonly TimeSpan? m_callbackTimeout;

        private readonly ILogger? m_logger;

        private readonly Action<string> m_close;

        private readonly Task m_writer;

        private long m_pendingCallbacks;

        private long m_pendingCallbackBytes;

        private int m_overflowLogged;

        private bool m_disposed;

        #endregion

        #region Constructors

        /// <param name="connectionId">The connection, for the log.</param>
        /// <param name="write">Encrypts, serializes and writes one message to the transport.</param>
        /// <param name="options">The server's callback delivery options.</param>
        /// <param name="callbackTimeout">How long one callback write may take before it is reported as timed out; null or zero for no limit.</param>
        /// <param name="logger">The server's logger.</param>
        /// <param name="close">Closes the connection with a reason; used by <see cref="CallbackOverflowPolicy.CloseConnection"/>.</param>
        public ConnectionOutbox(Guid connectionId, Func<WitMessage, Task> write, CallbackDeliveryOptions options,
            TimeSpan? callbackTimeout, ILogger? logger, Action<string> close)
        {
            m_connectionId = connectionId;
            m_write = write;
            m_options = options;
            m_callbackTimeout = callbackTimeout;
            m_logger = logger;
            m_close = close;

            m_writer = Task.Run(WriteAllAsync);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Queues a response or a handshake reply. Never refused.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Completes when the frame was written (or failed; the status says which).</returns>
        public Task<CallbackDeliveryStatus> EnqueueResponse(WitMessage message)
        {
            var frame = new OutboundFrame(message, false, 0);

            if (!m_frames.Writer.TryWrite(frame))
                frame.Completion.TrySetResult(CallbackDeliveryStatus.SendFailed);

            return frame.Completion.Task;
        }

        /// <summary>
        /// Queues a callback, subject to the bounds and the overflow policy.
        /// </summary>
        /// <param name="message">The callback message.</param>
        /// <param name="completion">The send's final status, when the callback was accepted.</param>
        /// <param name="refusal">Why it was not, otherwise.</param>
        /// <returns>True when the callback is in the queue.</returns>
        public bool TryEnqueueCallback(WitMessage message, out Task<CallbackDeliveryStatus> completion, out CallbackDeliveryStatus refusal)
        {
            var bytes = message.Data?.Length ?? 0;

            if (IsOverBound(bytes))
            {
                switch (m_options.OverflowPolicy)
                {
                    case CallbackOverflowPolicy.CloseConnection:
                        m_logger?.LogWarning("Callback queue of client {ClientId} is full ({Pending} pending, {PendingBytes} bytes); closing the connection",
                            m_connectionId, Volatile.Read(ref m_pendingCallbacks), Volatile.Read(ref m_pendingCallbackBytes));
                        completion = Task.FromResult(CallbackDeliveryStatus.QueueFull);
                        refusal = CallbackDeliveryStatus.QueueFull;
                        m_close("callback queue full");
                        return false;

                    case CallbackOverflowPolicy.DropNewest:
                        LogOverflowOnce("dropping the newest callback");
                        completion = Task.FromResult(CallbackDeliveryStatus.QueueFull);
                        refusal = CallbackDeliveryStatus.QueueFull;
                        return false;

                    default:
                        LogOverflowOnce("queuing anyway");
                        break;
                }
            }

            var frame = new OutboundFrame(message, true, bytes);
            Interlocked.Increment(ref m_pendingCallbacks);
            Interlocked.Add(ref m_pendingCallbackBytes, bytes);

            if (!m_frames.Writer.TryWrite(frame))
            {
                Interlocked.Decrement(ref m_pendingCallbacks);
                Interlocked.Add(ref m_pendingCallbackBytes, -bytes);
                completion = Task.FromResult(CallbackDeliveryStatus.SendFailed);
                refusal = CallbackDeliveryStatus.SendFailed;
                return false;
            }

            completion = frame.Completion.Task;
            refusal = CallbackDeliveryStatus.Queued;
            return true;
        }

        private bool IsOverBound(int bytes)
        {
            if (m_options.MaxPendingCallbacks > 0 && Volatile.Read(ref m_pendingCallbacks) >= m_options.MaxPendingCallbacks)
                return true;

            if (m_options.MaxPendingCallbackBytes > 0 && Volatile.Read(ref m_pendingCallbackBytes) + bytes > m_options.MaxPendingCallbackBytes)
                return true;

            return false;
        }

        private void LogOverflowOnce(string action)
        {
            if (Interlocked.Exchange(ref m_overflowLogged, 1) != 0)
                return;

            m_logger?.LogWarning("Callback queue of client {ClientId} passed its bound ({Pending} pending, {PendingBytes} bytes); {Action}",
                m_connectionId, Volatile.Read(ref m_pendingCallbacks), Volatile.Read(ref m_pendingCallbackBytes), action);
        }

        private async Task WriteAllAsync()
        {
            try
            {
                while (await m_frames.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (m_frames.Reader.TryRead(out var frame))
                        await WriteFrameAsync(frame).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                m_logger?.LogError(e, "Outbound writer of client {ClientId} failed", m_connectionId);
            }
        }

        private async Task WriteFrameAsync(OutboundFrame frame)
        {
            if (frame.IsCallback)
            {
                Interlocked.Decrement(ref m_pendingCallbacks);
                Interlocked.Add(ref m_pendingCallbackBytes, -frame.Bytes);
                Volatile.Write(ref m_overflowLogged, 0);
            }

            if (m_disposed)
            {
                frame.Completion.TrySetResult(CallbackDeliveryStatus.SendFailed);
                return;
            }

            var status = CallbackDeliveryStatus.Sent;
            try
            {
                var write = m_write(frame.Message);

                if (frame.IsCallback && m_callbackTimeout is { } timeout && timeout > TimeSpan.Zero)
                {
                    try
                    {
                        await write.WaitAsync(timeout).ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        status = CallbackDeliveryStatus.SendTimedOut;
                        m_logger?.LogWarning("Callback to client {ClientId} timed out", m_connectionId);

                        if (m_options.OverflowPolicy == CallbackOverflowPolicy.CloseConnection)
                            m_close("callback send timed out");

                        // The write still owns the transport: the next frame must not start
                        // before it ends, or the frames would interleave on the wire.
                        await write.ConfigureAwait(false);
                    }
                }
                else
                {
                    await write.ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                status = CallbackDeliveryStatus.SendFailed;
                m_logger?.LogError(e, "Failed to send message to client {ClientId}", m_connectionId);
            }

            frame.Completion.TrySetResult(status);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;
            m_frames.Writer.TryComplete();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Callbacks waiting in the queue.
        /// </summary>
        public long PendingCallbacks => Volatile.Read(ref m_pendingCallbacks);

        /// <summary>
        /// Payload bytes of the callbacks waiting in the queue.
        /// </summary>
        public long PendingCallbackBytes => Volatile.Read(ref m_pendingCallbackBytes);

        /// <summary>
        /// The writer task, for tests that wait for the queue to drain.
        /// </summary>
        internal Task Writer => m_writer;

        #endregion

        #region Nested Types

        private sealed class OutboundFrame
        {
            public OutboundFrame(WitMessage message, bool isCallback, int bytes)
            {
                Message = message;
                IsCallback = isCallback;
                Bytes = bytes;
                Completion = new TaskCompletionSource<CallbackDeliveryStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public WitMessage Message { get; }

            public bool IsCallback { get; }

            public int Bytes { get; }

            public TaskCompletionSource<CallbackDeliveryStatus> Completion { get; }
        }

        #endregion
    }
}
