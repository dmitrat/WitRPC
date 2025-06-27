using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Client.Rest.Exceptions
{
    public class WitExceptionRestRequest : WitException
    {
        public WitExceptionRestRequest(WitRequest requestBase)
            : this(requestBase, null, null)
        {
        }

        public WitExceptionRestRequest(WitRequest requestBase, string message)
            : this(requestBase, message, null)
        {

        }

        public WitExceptionRestRequest(WitRequest requestBase, string? message, Exception? innerException)
            : base(message, innerException)
        {
            RequestBase = requestBase;
        }

        public WitRequest RequestBase { get; }
    }
}
