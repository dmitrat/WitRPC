using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Server.Rest.Exceptions;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Rest.Utils
{
    public static class RequestUtils
    {
        public static WitRequest? RestoreFromGet(this HttpListenerRequest? request,
            IAccessTokenValidator tokenValidator, IMessageSerializer serializer)
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

            return methodName.CreateRequestRaw(parameters, serializer);

        }

        public static WitRequest? RestoreFromPost(this HttpListenerRequest? request, IAccessTokenValidator tokenValidator, IMessageSerializer serializer)
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

                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
                if (dictionary != null)
                    return methodName.CreateRequestRaw(dictionary.Values.ToArray(), serializer); 

                var list = JsonSerializer.Deserialize<List<object>>(data);
                if (list != null)
                    return methodName.CreateRequestRaw(list, serializer);
            }

            return null;

        }

        private static void CheckAuthorization(this HttpListenerRequest? request, IAccessTokenValidator tokenValidator)
        {
            var token = request.GetAuthorizationToken();

            if (string.IsNullOrEmpty(token) && !tokenValidator.IsRequestTokenValid(""))
                throw new WitExceptionRest("Authorization header is empty");

            if (!string.IsNullOrEmpty(token) && !tokenValidator.IsRequestTokenValid(token))
                throw new WitExceptionRest("Authorization failed, token is not valid");
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
