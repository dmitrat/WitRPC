using ProtoBuf;
using OutWit.Common.Abstract;

namespace OutWit.Common.ProtoBuf.Messages
{
    [ProtoContract]
    public class ProtoBufMessage : ModelBase
    {
        #region Contructors

        private ProtoBufMessage() :
            this("", false)
        {

        }

        public ProtoBufMessage(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return IsError ? $"ERROR: {Message}" : Message;
        }

        #endregion

        #region Model Base
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ProtoBufMessage message))
                return false;

            return IsError == message.IsError && 
                   Message == message.Message;
        }

        public override ModelBase Clone()
        {
            return new ProtoBufMessage(Message, IsError);
        }
        #endregion

        #region Properties

        [ProtoMember(1)]
        public string Message { get; private set; }

        [ProtoMember(2)]
        public bool IsError { get; private set; }

        #endregion
    }
}
