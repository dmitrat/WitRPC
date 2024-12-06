using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitComException : Exception
    {
        public WitComException() 
            : this(null, null)
        {

        }

        public WitComException(string message) 
            : this(message, null)
        {

        }

        public WitComException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
