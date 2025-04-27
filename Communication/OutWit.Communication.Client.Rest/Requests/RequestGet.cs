using System;
using System.Collections.Generic;
using System.Linq;
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
    public class RequestGet : ModelBase, IRequestMessage
    {
        #region Constructors

        public RequestGet(RestClientTransportOptions options, WitComRequest requestBase, string token)
        {
            Options = options;
            RequestBase = requestBase;
            Token = token;
            Method = HttpMethod.Get;

            CheckRequest();
        }

        #endregion

        #region Functions

        private void CheckRequest()
        {
            if (RequestBase.GenericArguments.Length > 0 || RequestBase.GenericArgumentsByName.Length > 0)
                throw new WitComExceptionRestRequest(RequestBase,
                    "Rest Get request cannot be constructed from generic method parameters");

            if (Options.Mode == RestClientRequestModes.PostOnly)
                throw new WitComExceptionRestRequest(RequestBase,
                    "Only post request allowed");

            IReadOnlyList<Type?> types = RequestBase.ParameterTypes;
            if (types.Count == 0)
                types = RequestBase.ParameterTypesByName.Select(parameterType => (Type?)parameterType).ToArray();

            if (Options.Mode == RestClientRequestModes.AllowGetForMethodsWithoutParameters && types.Count > 0)
                throw new WitComExceptionRestRequest(RequestBase, 
                    "Only get requests for methods without parameters allowed");


            if (Options.Mode == RestClientRequestModes.AllowGetForMethodsWithSingleParameter && types.Count > 1)
                throw new WitComExceptionRestRequest(RequestBase,
                    "Only get requests for methods with single parameter allowed");

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if(type == null)
                    continue;
                if (!type.CanAppend())
                    throw new WitComExceptionRestRequest(RequestBase,
                        $"Parameter of type {type} is not accepted to construct Get request");
            }
        }

        #endregion

        #region IRequest

        public AuthenticationHeaderValue? BuildHeader()
        {
            return new AuthenticationHeaderValue("Bearer", Token);
        }

        public virtual HttpContent? BuildContent()
        {
            return null;
        }

        public Uri Build()
        {
            var host = Options.Host?.AppendPath(RequestBase.MethodName);

            return new UriBuilder(host!.BuildConnection(false))
            {
                Path = host!.Path,
                Query = GetQuery()
            }.Uri;
        }

        private string GetQuery()
        {
            var builder = new QueryBuilder();

            for (int i = 0; i < RequestBase.Parameters.Length; i++)
                builder = builder.AddParameter($"param{(i + 1)}", RequestBase.Parameters[i]);

            return builder.AsStringAsync()
                .GetAwaiter()
                .GetResult();

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
