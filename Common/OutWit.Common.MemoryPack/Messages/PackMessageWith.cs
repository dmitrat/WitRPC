using System;
using MemoryPack;
using OutWit.Common.Abstract;

namespace OutWit.Common.MemoryPack.Messages
{
    [MemoryPackable]
    public partial class PackMessageWith<TData> : PackMessage
        where TData :class
    {
        #region Contructors

        private PackMessageWith() :
            this("", false)
        {

        }

        public PackMessageWith(string message, bool isError) : 
            base(message, isError)
        {
            Data = null;
        }

        public PackMessageWith(PackMessage message) :
            base(message.Message, message.IsError)
        {
            Data = null;
        }

        [MemoryPackConstructor]
        public PackMessageWith(string message, bool isError, TData? data) :
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
            if (!(modelBase is PackMessageWith<TData> message))
                return false;

            return base.Is(message, tolerance) &&
                   CompareData(message.Data, tolerance);
        }

        public override ModelBase Clone()
        {
            return new PackMessageWith<TData>(Message, IsError, CloneData());
        }

        #endregion

        #region Properties

        public TData? Data { get;}

        #endregion
    }
}
