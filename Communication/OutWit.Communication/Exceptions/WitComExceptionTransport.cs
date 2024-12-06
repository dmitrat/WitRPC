using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitComExceptionTransport : WitComException
    {
        public WitComExceptionTransport()
            : this(null, null)
        {

        }

        public WitComExceptionTransport(string message)
            : this(message, null)
        {

        }

        public WitComExceptionTransport(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
