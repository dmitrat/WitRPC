using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Server.Rest.Exceptions
{
    public class WitComExceptionRest : WitComException
    {
        public WitComExceptionRest()
            : this(null, null)
        {
        }

        public WitComExceptionRest(string message)
            : this(message, null)
        {

        }

        public WitComExceptionRest(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
