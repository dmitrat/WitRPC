using MemoryPack;
using OutWit.Common.Abstract;

namespace OutWit.Common.MemoryPack.Messages
{
    [MemoryPackable]
    public partial class PackMessage : ModelBase
    {
        #region Contructors

        private PackMessage() :
            this("", false)
        {

        }

        [MemoryPackConstructor]
        public PackMessage(string message, bool isError)
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
            if (!(modelBase is PackMessage message))
                return false;

            return IsError == message.IsError && 
                   Message == message.Message;
        }

        public override ModelBase Clone()
        {
            return new PackMessage(Message, IsError);
        }
        #endregion

        #region Properties

        public string Message { get;}

        public bool IsError { get;}

        #endregion
    }
}
