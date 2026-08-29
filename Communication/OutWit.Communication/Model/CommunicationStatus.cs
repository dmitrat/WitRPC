using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Model
{
    public enum CommunicationStatus : int
    {
        Unknown = 0,

        Ok = 200,

        BadRequest = 400,

        /// <summary>
        /// The client gave up waiting for a response. Client-local: the server
        /// may or may not have executed the call, so a retry is safe only for
        /// idempotent work -- but unlike a server fault, retrying can succeed.
        /// </summary>
        Timeout = 408,

        /// <summary>
        /// The service itself failed while executing the call. A retry would
        /// re-run failing business logic; not retryable by default.
        /// </summary>
        InternalServerError = 500,

        /// <summary>
        /// The request never made it to a response: the connection dropped, the
        /// send failed, or the reply was unreadable. Client-local and retryable.
        /// </summary>
        TransportError = 503,

        Unauthorized = 561
    }
}
