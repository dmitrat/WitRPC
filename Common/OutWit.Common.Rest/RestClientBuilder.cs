using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest
{
    public static class RestClientBuilder
    {
        #region Constants

        private const string BEARER_SCHEME = "Bearer";

        #endregion

        public static RestClientBase Create()
        {
            return new RestClient();
        }

        public static RestClientBase WithAuthorization(this RestClientBase me, string scheme, string? parameter)
        {
            me.SetAuthorization(scheme, parameter);

            return me;
        }

        public static RestClientBase WithBearer(this RestClientBase me, string? parameter)
        {
            return me.WithAuthorization(BEARER_SCHEME, parameter);
        }

        public static RestClientBase WithHeader(this RestClientBase me, string name, string? value)
        {
            me.SetHeader(name, value);

            return me;
        }
    }
}
