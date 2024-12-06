using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitComExceptionSerialization : WitComException
    {
        public WitComExceptionSerialization() 
            : this(null, null)
        {

        }

        public WitComExceptionSerialization(string message) 
            : this(message, null)
        {

        }

        public WitComExceptionSerialization(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
