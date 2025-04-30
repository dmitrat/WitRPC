using System;

namespace OutWit.Common.ProtoBuf
{
    public class ProtoBufOptions
    {
        internal ProtoBufOptions()
        {
            
        }

        public void RegisterSurrogate<TObject, TSurrogate>()
        {
            ProtoBufUtils.RegisterSurrogate<TObject, TSurrogate>();
        }
    }
}
