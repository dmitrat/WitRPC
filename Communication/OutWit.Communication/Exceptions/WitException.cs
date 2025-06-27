using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitException : Exception
    {
        public WitException() 
            : this(null, null)
        {

        }

        public WitException(string message) 
            : this(message, null)
        {

        }

        public WitException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
