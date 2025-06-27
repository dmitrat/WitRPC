using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitExceptionTransport : WitException
    {
        public WitExceptionTransport()
            : this(null, null)
        {

        }

        public WitExceptionTransport(string message)
            : this(message, null)
        {

        }

        public WitExceptionTransport(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
