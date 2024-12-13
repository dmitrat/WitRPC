using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Exceptions
{
    public class RestClientException : Exception
    {
        #region Constructors

        public RestClientException(HttpStatusCode statusCode, string? content)
        {
            StatusCode = statusCode;
            Content = content;

        } 

        #endregion

        #region Fucntions

        public override string ToString()
        {
            return $"StatusCode: {StatusCode}, Content: {Content ?? ""}";
        } 

        #endregion

        #region Properties

        public string? Content { get; }

        public HttpStatusCode StatusCode { get; } 

        #endregion
    }
}
