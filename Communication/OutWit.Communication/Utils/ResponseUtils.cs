using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Utils
{
    public static class ResponseUtils
    {
        public static WitResponse Success(this object? me, IMessageSerializer serializer)
        {
            return me == null
                ? WitResponse.Success(null) 
                : WitResponse.Success(serializer.Serialize(me, me.GetType()));
        }
    }
}
