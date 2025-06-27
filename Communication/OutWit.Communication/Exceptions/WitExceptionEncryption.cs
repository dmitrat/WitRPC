using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class WitExceptionEncryption : WitException
    {
        public WitExceptionEncryption() 
            : this(null, null)
        {

        }

        public WitExceptionEncryption(string message) 
            : this(message, null)
        {

        }

        public WitExceptionEncryption(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
