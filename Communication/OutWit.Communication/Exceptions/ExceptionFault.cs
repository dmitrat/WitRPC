using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Model;

namespace OutWit.Communication.Exceptions
{
    public class ExceptionFault : Exception
    {
        public ExceptionFault(CommunicationStatus status)
            : this( status,null, null)
        {

        }

        public ExceptionFault(CommunicationStatus status, string message)
            : this(status, message, null)
        {

        }

        public ExceptionFault(CommunicationStatus status, string? message, Exception? innerException)
            : base(message, innerException)
        {
            Status = status;
        }

        public CommunicationStatus Status { get; }
    }
}
