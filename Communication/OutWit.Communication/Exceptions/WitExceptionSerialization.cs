using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitExceptionSerialization : WitException
    {
        public WitExceptionSerialization() 
            : this(null, null)
        {

        }

        public WitExceptionSerialization(string message) 
            : this(message, null)
        {

        }

        public WitExceptionSerialization(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
