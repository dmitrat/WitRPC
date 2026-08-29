#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemoryPack;
using OutWit.Communication.Messages;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Serializers
{
    /// <summary>
    /// Registers the MemoryPack formatters for every wire model explicitly, at
    /// assembly load. Without this, the provider discovers formatters through
    /// reflection (<c>RegisterFormatter</c> looked up by name), which trimming
    /// removes -- under NativeAOT the handshake would then fail to deserialize.
    /// A direct generic registration is statically reachable, so the trimmer
    /// keeps it and no reflection runs at all.
    /// </summary>
    internal static class MemoryPackWireFormatters
    {
        #region Initialization

        [ModuleInitializer]
        [SuppressMessage("Usage", "CA2255",
            Justification = "Deliberate library-level registration: the formatters must exist before any " +
                            "deserialization call, and reflection-based discovery is removed by trimming.")]
        internal static void Register()
        {
            MemoryPackFormatterProvider.Register<WitMessage>();
            MemoryPackFormatterProvider.Register<WitRequest>();
            MemoryPackFormatterProvider.Register<WitResponse>();
            MemoryPackFormatterProvider.Register<WitRequestInitialization>();
            MemoryPackFormatterProvider.Register<WitResponseInitialization>();
            MemoryPackFormatterProvider.Register<WitRequestAuthorization>();
            MemoryPackFormatterProvider.Register<WitResponseAuthorization>();
            MemoryPackFormatterProvider.Register<ParameterType>();
            MemoryPackFormatterProvider.Register<DiscoveryMessage>();
        }

        #endregion
    }
}
#endif
