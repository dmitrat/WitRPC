using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Model;

namespace OutWit.Communication.Exceptions
{
    public class WitExceptionFault : WitException
    {
        public WitExceptionFault(CommunicationStatus status)
            : this( status,null, null)
        {

        }

        public WitExceptionFault(CommunicationStatus status, string message)
            : this(status, message, null)
        {

        }

        public WitExceptionFault(CommunicationStatus status, string? message, Exception? innerException)
            : base(message, innerException)
        {
            Status = status;
        }

        public CommunicationStatus Status { get; }
    }
}
