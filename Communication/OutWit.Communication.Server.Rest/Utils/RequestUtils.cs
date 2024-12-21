using System;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Server.Rest.Exceptions;

namespace OutWit.Communication.Server.Rest.Utils
{
    public static class RequestUtils
    {
        public static WitComRequest? RestoreFromGet(this HttpListenerRequest? request,
            IAccessTokenValidator tokenValidator)
        {
            if(request == null || request.Url == null || request.HttpMethod != HttpMethod.Get.Method)
                return null;

            request.CheckAuthorization(tokenValidator);
            
            var methodName = request.Url.AbsolutePath.Split('/').LastOrDefault();
            if(string.IsNullOrEmpty(methodName))
                return null;

            var parameters = new List<object>();
            for (int i = 0; i < request.QueryString.Keys.Count; i++)
            {
                var parameter = request.QueryString.Get(i);
                if(!string.IsNullOrEmpty(parameter))
                    parameters.Add(parameter);
            }

            return new WitComRequest
            {
                MethodName = methodName,
                Parameters = parameters.ToArray()
            };

        }

        public static WitComRequest? RestoreFromPost(this HttpListenerRequest? request, IAccessTokenValidator tokenValidator)
        {
            if (request == null || request.Url == null || request.HttpMethod != HttpMethod.Post.Method)
                return null;

            request.CheckAuthorization(tokenValidator);

            var methodName = request.Url.AbsolutePath.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(methodName))
                return null;

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                var data = reader.ReadToEnd();

                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                if (dictionary != null)
                    return new WitComRequest
                    {
                        MethodName = methodName,
                        Parameters = dictionary.Values.ToArray()
                    };

                var list = JsonConvert.DeserializeObject<List<object>>(data);
                if (list != null)
                    return new WitComRequest
                    {
                        MethodName = methodName,
                        Parameters = list.ToArray()
                    };
            }

            return null;

        }

        private static void CheckAuthorization(this HttpListenerRequest? request, IAccessTokenValidator tokenValidator)
        {
            var token = request.GetAuthorizationToken();

            if (string.IsNullOrEmpty(token) && !tokenValidator.IsRequestTokenValid(""))
                throw new WitComExceptionRest("Authorization header is empty");

            if (!string.IsNullOrEmpty(token) && !tokenValidator.IsRequestTokenValid(token))
                throw new WitComExceptionRest("Authorization failed, token is not valid");
        }

        private static string? GetAuthorizationToken(this HttpListenerRequest? request)
        {
            var headerString = request?.Headers[nameof(HttpRequestHeader.Authorization)];
            if(string.IsNullOrEmpty(headerString))
                return null;

            if(!AuthenticationHeaderValue.TryParse(headerString, out AuthenticationHeaderValue? header))
                return null;

            return header.Parameter;
        }
    }
}
