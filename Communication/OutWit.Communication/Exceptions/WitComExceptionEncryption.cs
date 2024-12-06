using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitComExceptionEncryption : WitComException
    {
        public WitComExceptionEncryption() 
            : this(null, null)
        {

        }

        public WitComExceptionEncryption(string message) 
            : this(message, null)
        {

        }

        public WitComExceptionEncryption(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
