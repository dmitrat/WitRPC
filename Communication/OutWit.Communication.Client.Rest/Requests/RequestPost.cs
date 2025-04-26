using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using OutWit.Common.Abstract;
using OutWit.Common.Rest;
using OutWit.Common.Rest.Interfaces;
using OutWit.Common.Rest.Utils;
using OutWit.Common.Values;
using OutWit.Communication.Client.Rest.Exceptions;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Client.Rest.Requests
{
    public class RequestPost : ModelBase, IRequestMessage
    {
        #region Constructors

        public RequestPost(RestClientTransportOptions options, WitComRequest requestBase, string token)
        {
            Options = options;
            RequestBase = requestBase;
            Token = token;
            Method = HttpMethod.Post;

            CheckRequest();
        }

        #endregion

        #region Functions

        private void CheckRequest()
        {
            if (RequestBase.GenericArguments.Count > 0 || RequestBase.GenericArgumentsByName.Count > 0)
                throw new WitComExceptionRestRequest(RequestBase,
                    "Rest Post request cannot be constructed from generic method parameters");
        }

        #endregion

        #region IRequest

        public AuthenticationHeaderValue? BuildHeader()
        {
            return new AuthenticationHeaderValue("Bearer", Token);
        }

        public virtual HttpContent? BuildContent()
        {
            var parameters = new Dictionary<string, object>();

            for (int i = 0; i < RequestBase.Parameters.Count; i++)
                parameters.Add($"param{(i + 1)}", RequestBase.Parameters[i]);

            return parameters.JsonContent();
        }

        public Uri Build()
        {
            var host = Options.Host?.AppendPath(RequestBase.MethodName);

            return new UriBuilder(host!.BuildConnection(false))
            {
                Path = host!.Path
            }.Uri;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not RequestGet request)
                return false;

            return Options.Is(request.Options) &&
                   RequestBase.Is(request.RequestBase) &&
                   Token.Is(request.Token);
        }

        public override RequestGet Clone()
        {
            return new RequestGet(Options, RequestBase, Token);
        }

        #endregion

        #region Properties

        public RestClientTransportOptions Options { get; }

        public WitComRequest RequestBase { get; }

        public string Token { get; }

        public HttpMethod Method { get; }

        #endregion

    }
}
