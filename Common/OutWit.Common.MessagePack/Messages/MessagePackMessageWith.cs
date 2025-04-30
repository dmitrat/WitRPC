using System;
using MessagePack;
using OutWit.Common.Abstract;

namespace OutWit.Common.MessagePack.Messages
{
    [MessagePackObject]
    public class MessagePackMessageWith<TData> : MessagePackMessage
        where TData :class
    {
        #region Contructors

        private MessagePackMessageWith() :
            this("", false)
        {

        }

        public MessagePackMessageWith(string message, bool isError) : 
            base(message, isError)
        {
            Data = null;
        }

        public MessagePackMessageWith(MessagePackMessage messagePackMessage) :
            base(messagePackMessage.Message, messagePackMessage.IsError)
        {
            Data = null;
        }

        [SerializationConstructor]
        public MessagePackMessageWith(string message, bool isError, TData data) :
            base(message, isError)
        {
            Data = data;
        }

        #endregion

        #region Functions

        private bool CompareData(TData data, double tolerance = DEFAULT_TOLERANCE)
        {
            if (Data is ModelBase modelData1 && data is ModelBase modelData2)
                return modelData1.Is(modelData2, tolerance);

            return Data.Equals(data);
        }

        private TData CloneData()
        {
            if (Data is ICloneable cloneable)
                return cloneable.Clone() as TData;

            return Data;
        }

        #endregion

        #region Model Base
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MessagePackMessageWith<TData> message))
                return false;

            return base.Is(message, tolerance) &&
                   CompareData(message.Data, tolerance);
        }

        public override ModelBase Clone()
        {
            return new MessagePackMessageWith<TData>(Message, IsError, CloneData());
        }

        #endregion

        #region Properties

        [Key(2)]
        public TData Data { get;}

        #endregion
    }
}
