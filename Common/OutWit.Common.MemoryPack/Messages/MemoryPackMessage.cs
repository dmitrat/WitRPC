using MemoryPack;
using OutWit.Common.Abstract;

namespace OutWit.Common.MemoryPack.Messages
{
    [MemoryPackable]
    public partial class MemoryPackMessage : ModelBase
    {
        #region Contructors

        private MemoryPackMessage() :
            this("", false)
        {

        }

        [MemoryPackConstructor]
        public MemoryPackMessage(string message, bool isError)
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
            if (!(modelBase is MemoryPackMessage message))
                return false;

            return IsError == message.IsError && 
                   Message == message.Message;
        }

        public override ModelBase Clone()
        {
            return new MemoryPackMessage(Message, IsError);
        }
        #endregion

        #region Properties

        public string Message { get;}

        public bool IsError { get;}

        #endregion
    }
}
