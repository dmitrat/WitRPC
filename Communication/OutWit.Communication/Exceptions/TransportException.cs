using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class TransportException : Exception
    {
        public TransportException()
            : this(null, null)
        {

        }

        public TransportException(string message)
            : this(message, null)
        {

        }

        public TransportException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
