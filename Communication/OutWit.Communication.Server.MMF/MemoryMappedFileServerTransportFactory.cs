using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.MMF;

namespace OutWit.Communication.Server.MMF
{
    /// <summary>
    /// Publishes one memory-mapped channel at a time under a fixed name.
    /// <para>
    /// The transport is one-to-one by design: a channel has exactly one client,
    /// and the next client is served by a fresh channel created after the
    /// previous one is gone. <see cref="NewClientConnected"/> is raised when a
    /// client has actually attached, not when a channel merely exists.
    /// </para>
    /// </summary>
    public class MemoryMappedFileServerTransportFactory : ITransportServerFactory
    {
        #region Constants

        /// <summary>
        /// The previous client may still be closing its handles when the next
        /// channel is created under the same name; retry quietly for this long.
        /// </summary>
        private const int CREATE_RETRY_WINDOW_MS = 5000;

        private const int CREATE_RETRY_DELAY_MS = 50;

        private const int CREATE_RETRY_IDLE_DELAY_MS = 500;

        /// <summary>
        /// How long a ready channel waits for the client that claimed its slot to
        /// actually attach. A client claims the slot and then says hello within
        /// microseconds; a claim that never turns into a hello (a client that died
        /// between the two) recycles the channel rather than parking the factory.
        /// </summary>
        private const int ATTACH_TIMEOUT_MS = 10000;

        #endregion

        #region Events

        public event TransportFactoryEventHandler NewClientConnected = delegate { };

        #endregion

        #region Fields

        private readonly object m_syncRoot = new();

        private readonly Semaphore m_slot;

        private MemoryMappedFileServerTransport? m_active;

        private TaskCompletionSource<bool>? m_slotReleased;

        #endregion

        #region Constructors

        /// <summary>
        /// Validates the options. No kernel object is created until
        /// <see cref="StartWaitingForConnection"/>.
        /// </summary>
        /// <param name="options">Name and size of the channel.</param>
        /// <exception cref="WitExceptionTransport">Name is empty or size is too small.</exception>
        public MemoryMappedFileServerTransportFactory(MemoryMappedFileServerTransportOptions options)
        {
            Options = options;

            if (string.IsNullOrEmpty(Options.Name))
                throw new WitExceptionTransport("Memory mapped file name cannot be empty");

            if (Options.Size < MmfChannelLayout.MIN_FILE_SIZE)
                throw new WitExceptionTransport($"Memory mapped file size must be at least {MmfChannelLayout.MIN_FILE_SIZE} bytes");

            // The connection slot outlives individual channel instances: it is how
            // a fresh, ready channel offers itself to exactly one client. Drain any
            // permit left by a previous run so it starts empty.
            m_slot = new Semaphore(0, 1, MmfChannelLayout.SlotName(Options.Name!));
            DrainSlot();
        }

        #endregion

        #region Functions

        public void StartWaitingForConnection(ILogger? logger)
        {
            lock (m_syncRoot)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(MemoryMappedFileServerTransportFactory));

                if (CancellationTokenSource != null)
                    throw new InvalidOperationException("MMF listener is already running");

                var cancellationTokenSource = new CancellationTokenSource();

                CancellationTokenSource = cancellationTokenSource;
                AcceptLoopTask = Task.Run(() => AcceptLoopAsync(cancellationTokenSource.Token, logger));
            }
        }

        public void StopWaitingForConnection()
        {
            CancellationTokenSource? cancellationTokenSource;
            Task? acceptLoopTask;

            lock (m_syncRoot)
            {
                cancellationTokenSource = CancellationTokenSource;
                acceptLoopTask = AcceptLoopTask;

                CancellationTokenSource = null;
                AcceptLoopTask = null;
            }

            if (cancellationTokenSource == null && acceptLoopTask == null)
                return;

            try
            {
                cancellationTokenSource?.Cancel(false);
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                acceptLoopTask?.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }

            cancellationTokenSource?.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken, ILogger? logger)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MemoryMappedFileServerTransport? transport = await CreateTransportAsync(cancellationToken, logger).ConfigureAwait(false);
                if (transport == null)
                    return;

                // The channel is listening (its reader ran from construction).
                // Offer it to one client by posting a single permit.
                DrainSlot();
                m_slot.Release();

                bool attached;
                try
                {
                    using var attachTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    attachTimeout.CancelAfter(ATTACH_TIMEOUT_MS);

                    attached = await transport.InitializeConnectionAsync(attachTimeout.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "MMF SERVER LOOP ERROR");
                    attached = false;
                }

                if (!attached)
                {
                    // No client claimed and attached in time (or we are stopping).
                    // Drop the offer and recycle.
                    DrainSlot();
                    transport.Dispose();
                    continue;
                }

                var released = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                lock (m_syncRoot)
                {
                    m_active = transport;
                    m_slotReleased = released;
                }

                transport.Disconnected += OnTransportDisconnected;

                NewClientConnected(transport);

                // The connection is now registered above; acknowledge the hello so
                // the client's connect completes only once the server can route its
                // first message.
                await transport.ConfirmAttachedAsync().ConfigureAwait(false);

                try
                {
                    await released.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Stop only stops accepting; the attached client is left to
                    // whoever owns the connection, as with every other transport.
                    return;
                }
            }
        }

        private void DrainSlot()
        {
            while (m_slot.WaitOne(0))
            {
            }
        }

        private async Task<MemoryMappedFileServerTransport?> CreateTransportAsync(CancellationToken cancellationToken, ILogger? logger)
        {
            var stopwatch = Stopwatch.StartNew();
            var reported = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    return new MemoryMappedFileServerTransport(Options);
                }
                catch (Exception ex)
                {
                    int delay = CREATE_RETRY_DELAY_MS;

                    if (stopwatch.ElapsedMilliseconds < CREATE_RETRY_WINDOW_MS)
                    {
                        logger?.LogDebug(ex, "MMF channel {Name} is not free yet, retrying", Options.Name);
                    }
                    else
                    {
                        if (!reported)
                        {
                            logger?.LogError(ex, "MMF channel {Name} could not be created for {Elapsed} ms; still retrying", Options.Name, stopwatch.ElapsedMilliseconds);
                            reported = true;
                        }

                        delay = CREATE_RETRY_IDLE_DELAY_MS;
                    }

                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Event Handlers

        private void OnTransportDisconnected(Guid sender)
        {
            TaskCompletionSource<bool>? released = null;

            lock (m_syncRoot)
            {
                if (m_active != null && m_active.Id == sender)
                {
                    m_active = null;
                    released = m_slotReleased;
                    m_slotReleased = null;
                }
            }

            released?.TrySetResult(true);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            StopWaitingForConnection();

            MemoryMappedFileServerTransport? active;
            lock (m_syncRoot)
            {
                active = m_active;
                m_active = null;
            }

            active?.Dispose();

            m_slot.Dispose();
        }

        #endregion

        #region Properties

        IServerOptions ITransportServerFactory.Options => Options;

        private MemoryMappedFileServerTransportOptions Options { get; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private Task? AcceptLoopTask { get; set; }

        private bool IsDisposed { get; set; }

        #endregion
    }
}
