
namespace OutWit.Communication.Interfaces
{
    /// <summary>
    /// The one knob a serializer plugin needs: which serializer turns method
    /// parameters, results and event arguments into bytes. Implemented by the
    /// client and server builder options, so a plugin package ships a single
    /// generic <c>WithX()</c> extension that works on both without referencing
    /// either.
    /// </summary>
    public interface ISerializationOptions
    {
        /// <summary>
        /// The serializer for user payloads: parameters, return values and
        /// event arguments. The message envelope itself always travels as
        /// MemoryPack.
        /// </summary>
        IMessageSerializer ParametersSerializer { get; set; }
    }
}
