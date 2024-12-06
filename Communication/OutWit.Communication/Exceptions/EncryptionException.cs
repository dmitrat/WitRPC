using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Exceptions
{
    public class EncryptionException : Exception
    {
        public EncryptionException() 
            : this(null, null)
        {

        }

        public EncryptionException(string message) 
            : this(message, null)
        {

        }

        public EncryptionException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }
    }
}
