using System;

namespace OutWit.Communication.MMF
{
    /// <summary>
    /// The byte layout of a memory-mapped channel and the names of the kernel
    /// objects that go with it. Compiled into both the client and the server
    /// package, so the two ends can never disagree about where a frame lives.
    /// <para>
    /// File: a 64-byte header (magic, layout version, file size, region
    /// capacity), then two equal regions — client→server and server→client.
    /// Each region holds one frame at a time: a 16-byte frame header followed
    /// by the payload chunk. Messages larger than a region are chunked.
    /// </para>
    /// </summary>
    internal static class MmfChannelLayout
    {
        #region Constants

        /// <summary>"WMMF" — identifies a WitRPC channel, so an old server or an unrelated mapping is refused with a clear error rather than misread.</summary>
        public const int MAGIC = 0x464D4D57;

        public const int LAYOUT_VERSION = 1;

        public const int FILE_HEADER_SIZE = 64;

        public const int FRAME_HEADER_SIZE = 16;

        /// <summary>Below this the regions would be too small to carry a frame header and a byte.</summary>
        public const long MIN_FILE_SIZE = 1024;

        public const int FILE_OFFSET_MAGIC = 0;

        public const int FILE_OFFSET_VERSION = 4;

        public const int FILE_OFFSET_SIZE = 8;

        public const int FILE_OFFSET_CAPACITY = 16;

        public const int FRAME_OFFSET_CHUNK_LENGTH = 0;

        public const int FRAME_OFFSET_TOTAL_LENGTH = 4;

        public const int FRAME_OFFSET_CHUNK_OFFSET = 8;

        public const int FRAME_OFFSET_FLAGS = 12;

        /// <summary>
        /// Session-local namespace for every object. The 2.x transport put the
        /// events in <c>Global\</c> and the file nowhere, which could not work
        /// across sessions anyway; one consistent prefix makes the contract honest.
        /// </summary>
        private const string PREFIX = "Local\\";

        #endregion

        #region Geometry

        public static long RegionSize(long fileSize)
        {
            return (fileSize - FILE_HEADER_SIZE) / 2;
        }

        public static int Capacity(long fileSize)
        {
            return (int)Math.Min(RegionSize(fileSize) - FRAME_HEADER_SIZE, int.MaxValue);
        }

        public static long ClientToServerOffset(long fileSize)
        {
            return FILE_HEADER_SIZE;
        }

        public static long ServerToClientOffset(long fileSize)
        {
            return FILE_HEADER_SIZE + RegionSize(fileSize);
        }

        #endregion

        #region Names

        public static string FileName(string name)
        {
            return $"{PREFIX}{name}";
        }

        public static string ClientToServerReadyName(string name)
        {
            return $"{PREFIX}{name}_c2s_ready";
        }

        public static string ClientToServerFreeName(string name)
        {
            return $"{PREFIX}{name}_c2s_free";
        }

        public static string ServerToClientReadyName(string name)
        {
            return $"{PREFIX}{name}_s2c_ready";
        }

        public static string ServerToClientFreeName(string name)
        {
            return $"{PREFIX}{name}_s2c_free";
        }

        public static string ServerAliveName(string name)
        {
            return $"{PREFIX}{name}_server_alive";
        }

        public static string ClientAliveName(string name)
        {
            return $"{PREFIX}{name}_client_alive";
        }

        /// <summary>
        /// The connection slot: a max-count-1 semaphore owned by the factory. A
        /// ready server posts one permit; a client claims it before attaching.
        /// The permit is what makes the handoff atomic — a client can only ever
        /// attach to a fresh instance that posted a permit, never to a departing
        /// one that still holds the same named channel objects.
        /// </summary>
        public static string SlotName(string name)
        {
            return $"{PREFIX}{name}_slot";
        }

        #endregion
    }
}
