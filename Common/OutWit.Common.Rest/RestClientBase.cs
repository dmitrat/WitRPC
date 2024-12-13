using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OutWit.Common.Rest.Exceptions;
using OutWit.Common.Rest.Interfaces;
using OutWit.Common.Rest.Utils;

namespace OutWit.Common.Rest
{
    public class RestClientBase : IDisposable
    {
        #region Constructors

        protected RestClientBase()
        {
            HttpClient = new HttpClient();
        }

        #endregion

        #region Functions

        #region Get

        public async Task<TValue> GetAsync<TValue>(IRequest request)
            where TValue : class
        {
            return await GetAsync<TValue>(request.Build());
        }

        public async Task<TValue> GetAsync<TValue>(string request)
            where TValue : class
        {
            return await GetAsync<TValue>(new Uri(request));
        }

        public async Task<TValue> GetAsync<TValue>(Uri request)
            where TValue : class
        {
            var response = await HttpClient.GetAsync(request).ConfigureAwait(false);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new RestClientException(response.StatusCode, await response.Content.ReadAsStringAsync());

            return await response.DeserializeAsync<TValue>();
        }

        #endregion

        #region Post

        public async Task<TValue> PostAsync<TValue>(IRequestPost request)
            where TValue : class
        {
            return await PostAsync<TValue>(request.Build(), request.BuildContent());
        }

        public async Task<TValue> PostAsync<TValue>(string request, HttpContent? content)
            where TValue : class
        {
            return await PostAsync<TValue>(new Uri(request), content);
        }

        public async Task<TValue> PostAsync<TValue>(Uri request, HttpContent? content)
            where TValue : class
        {
            var response = await HttpClient.PostAsync(request, content).ConfigureAwait(false);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new RestClientException(response.StatusCode, await response.Content.ReadAsStringAsync());

            return await response.DeserializeAsync<TValue>();
        }

        #endregion

        #region Send

        public async Task<TValue> SendAsync<TValue>(IRequestMessage message)
            where TValue : class
        {
            return await SendAsync<TValue>(message.ToRequestMessage());
        }

        public async Task<TValue> SendAsync<TValue>(HttpRequestMessage message)
            where TValue : class
        {
            var response = await HttpClient.SendAsync(message).ConfigureAwait(false);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new RestClientException(response.StatusCode, await response.Content.ReadAsStringAsync());

#if DEBUG
            await using var stream = await response.Content.ReadAsStreamAsync();

            using var reader = new StreamReader(stream);

            var str = reader.ReadToEnd();

            Console.WriteLine(str);

            return JsonConvert.DeserializeObject<TValue>(str);
#else

            return await response.DeserializeAsync<TValue>();

#endif
        }

        #endregion

        #region Tools

        internal void SetAuthorization(string scheme, string? parameter)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
        }

        internal void SetHeader(string name, string? value)
        {
            HttpClient.DefaultRequestHeaders.Add(name, value);
        }

        #endregion

        #endregion

        #region IDisposable

        public void Dispose()
        {
            HttpClient.Dispose();
        }

        #endregion

        #region Properties

        protected HttpClient HttpClient { get; }

        #endregion
    }
}
