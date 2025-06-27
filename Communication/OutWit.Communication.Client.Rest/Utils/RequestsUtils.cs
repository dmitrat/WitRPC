using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Rest.Interfaces;
using OutWit.Communication.Client.Rest.Requests;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Client.Rest.Utils
{
    public static class RequestsUtils
    {
        public static IRequestMessage? ConstructGetRequest(this WitRequest? me, RestClientTransportOptions options, IAccessTokenProvider tokenProvider)
        {
            if (me == null)
                return null;

            try
            {
                return new RequestGet(options, me, tokenProvider.GetToken());
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static IRequestMessage? ConstructPostRequest(this WitRequest? me, RestClientTransportOptions options, IAccessTokenProvider tokenProvider)
        {
            if (me == null)
                return null;

            try
            {
                return new RequestPost(options, me, tokenProvider.GetToken());
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
