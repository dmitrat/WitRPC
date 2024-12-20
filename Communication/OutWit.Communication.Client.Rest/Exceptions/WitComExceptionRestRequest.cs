using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Client.Rest.Exceptions
{
    public class WitComExceptionRestRequest : WitComException
    {
        public WitComExceptionRestRequest(WitComRequest requestBase)
            : this(requestBase, null, null)
        {
        }

        public WitComExceptionRestRequest(WitComRequest requestBase, string message)
            : this(requestBase, message, null)
        {

        }

        public WitComExceptionRestRequest(WitComRequest requestBase, string? message, Exception? innerException)
            : base(message, innerException)
        {
            RequestBase = requestBase;
        }

        public WitComRequest RequestBase { get; }
    }
}
