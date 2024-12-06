using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class CommunicationException : Exception
    {
        public CommunicationException() 
            : this(null, null)
        {

        }

        public CommunicationException(string message) 
            : this(message, null)
        {

        }

        public CommunicationException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
