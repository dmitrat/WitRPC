using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Server.Rest.Exceptions
{
    public class WitExceptionRest : WitException
    {
        public WitExceptionRest()
            : this(null, null)
        {
        }

        public WitExceptionRest(string message)
            : this(message, null)
        {

        }

        public WitExceptionRest(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
