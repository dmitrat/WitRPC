using System;
using ProtoBuf;
using OutWit.Common.Abstract;

namespace OutWit.Common.ProtoBuf.Messages
{
    [ProtoContract]
    public class ProtoBufMessageWith<TData> : ProtoBufMessage
        where TData :class
    {
        #region Contructors

        private ProtoBufMessageWith() :
            this("", false)
        {

        }

        public ProtoBufMessageWith(string message, bool isError) : 
            base(message, isError)
        {
            Data = null;
        }

        public ProtoBufMessageWith(ProtoBufMessage bufMessage) :
            base(bufMessage.Message, bufMessage.IsError)
        {
            Data = null;
        }

        public ProtoBufMessageWith(string message, bool isError, TData? data) :
            base(message, isError)
        {
            Data = data;
        }

        #endregion

        #region Functions

        private bool CompareData(TData? data, double tolerance = DEFAULT_TOLERANCE)
        {
            if (Data is ModelBase modelData1 && data is ModelBase modelData2)
                return modelData1.Is(modelData2, tolerance);

            return Data?.Equals(data) == true;
        }

        private TData? CloneData()
        {
            if (Data is ICloneable cloneable)
                return cloneable.Clone() as TData;

            return Data;
        }

        #endregion

        #region Model Base
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ProtoBufMessageWith<TData> message))
                return false;

            return base.Is(message, tolerance) &&
                   CompareData(message.Data, tolerance);
        }

        public override ModelBase Clone()
        {
            return new ProtoBufMessageWith<TData>(Message, IsError, CloneData());
        }

        #endregion

        #region Properties

        [ProtoMember(3)]
        public TData? Data { get; private set; }

        #endregion
    }
}
