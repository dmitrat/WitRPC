using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using OutWit.Communication.Server.Rest;
using OutWit.Communication.Tests.Mock;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Communication
{
    /// <summary>
    /// The REST transport's reason to exist: a caller that is not WitRPC at all.
    /// Every test here speaks to the server with a bare <see cref="HttpClient"/>
    /// and plain JSON -- no envelope, no type names, no encoding -- and reads
    /// the result back as plain JSON.
    /// </summary>
    [TestFixture]
    public class CommunicationTestsRestInterop
    {
        #region Constants

        private const string TOKEN = "rest-token";

        #endregion

        #region Fields

        private static readonly HttpClient s_http = new();

        #endregion

        #region Setup

        private static string NextUrl()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return $"http://localhost:{port}/api/";
        }

        private static WitServerRest StartServer(string url, bool auth)
        {
            var server = WitServerRestBuilder.Build(options =>
            {
                options.WithUrl(url);
                options.WithService<IService>(new MockService());

                if (auth)
                    options.WithAccessToken(TOKEN);
                else
                    options.WithoutAuthorization();
            });

            server.StartWaitingForConnection();
            return server;
        }

        private static async Task<(HttpStatusCode status, string body, string? contentType)> PostAsync(string url, string method, string json, string? token = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url + method)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await s_http.SendAsync(request);
            return (response.StatusCode, await response.Content.ReadAsStringAsync(), response.Content.Headers.ContentType?.MediaType);
        }

        private static async Task<(HttpStatusCode status, string body)> GetAsync(string url, string methodAndQuery)
        {
            using var response = await s_http.GetAsync(url + methodAndQuery);
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        #endregion

        #region Request Shape Tests

        [Test]
        public async Task NamedArgumentsInAJsonObjectTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, contentType) = await PostAsync(url, "RequestData", "{\"message\":\"hello\"}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(contentType, Is.EqualTo("application/json"));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hello"));
        }

        [Test]
        public async Task NamedArgumentsAreMatchedRegardlessOfOrderAndCaseTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var expected = new MockService().StartProcessing(new ComplexNumber<int, int>(2, 3), 4);

            var (status, body, _) = await PostAsync(url, "StartProcessing", "{\"ITERATIONS\":4,\"number\":{\"A\":2,\"B\":3}}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));

            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("A").GetInt32(), Is.EqualTo(expected.A));
            Assert.That(document.RootElement.GetProperty("B").GetInt32(), Is.EqualTo(expected.B));
        }

        [Test]
        public async Task PositionalArgumentsInAJsonArrayTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, _) = await PostAsync(url, "RequestData", "[\"hello\"]");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hello"));
        }

        [Test]
        public async Task NullArgumentsBindAsNullsTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var expected = new MockService().RequestWithMultipleNullableParams(null, null, null);

            var (status, body, _) = await PostAsync(url, "RequestWithMultipleNullableParams", "{\"first\":null,\"second\":null,\"third\":null}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo(expected));
        }

        [Test]
        public async Task QueryStringByNameTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body) = await GetAsync(url, "RequestData?message=hi%20there");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hi there"));
        }

        [Test]
        public async Task QueryStringByPositionTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body) = await GetAsync(url, "RequestData?param1=hi");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hi"));
        }

        [Test]
        public async Task QueryStringBindsNumbersAndObjectsAgainstDeclaredTypesTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var expected = new MockService().StartProcessing(new ComplexNumber<int, int>(5, 6), 2);

            var (status, body) = await GetAsync(url, "StartProcessing?number=" + Uri.EscapeDataString("{\"A\":5,\"B\":6}") + "&iterations=2");

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));

            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("A").GetInt32(), Is.EqualTo(expected.A));
            Assert.That(document.RootElement.GetProperty("B").GetInt32(), Is.EqualTo(expected.B));
        }

        #endregion

        #region Response Shape Tests

        [Test]
        public async Task VoidMethodAnswersNoContentTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, _) = await PostAsync(url, "ReportError", "{\"error\":\"nothing serious\"}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(body, Is.Empty);
        }

        [Test]
        public async Task UnknownMethodIsNotFoundWithAnErrorObjectTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, contentType) = await PostAsync(url, "NoSuchMethod", "{}");

            Assert.That(status, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(contentType, Is.EqualTo("application/json"));

            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("error").GetString(), Does.Contain("NoSuchMethod"));
            Assert.That(document.RootElement.GetProperty("status").GetString(), Is.EqualTo("BadRequest"));
        }

        [Test]
        public async Task WrongArityIsBadRequestTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, _) = await PostAsync(url, "RequestData", "[\"one\",\"two\"]");

            Assert.That(status, Is.EqualTo(HttpStatusCode.BadRequest));
            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("error").GetString(), Does.Contain("2 argument"));
        }

        [Test]
        public async Task InvalidJsonIsBadRequestTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: false);

            var (status, body, _) = await PostAsync(url, "RequestData", "{not json");

            Assert.That(status, Is.EqualTo(HttpStatusCode.BadRequest));
            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("error").GetString(), Does.Contain("JSON"));
        }

        [Test]
        public async Task WrongTokenIsUnauthorizedWithAnErrorObjectTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var (status, body, _) = await PostAsync(url, "RequestData", "{\"message\":\"hello\"}", token: "wrong");

            Assert.That(status, Is.EqualTo(HttpStatusCode.Unauthorized));
            using var document = JsonDocument.Parse(body);
            Assert.That(document.RootElement.GetProperty("status").GetString(), Is.EqualTo("Unauthorized"));
        }

        [Test]
        public async Task RightTokenIsAcceptedTest()
        {
            var url = NextUrl();
            using var server = StartServer(url, auth: true);

            var (status, body, _) = await PostAsync(url, "RequestData", "{\"message\":\"hello\"}", token: TOKEN);

            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonSerializer.Deserialize<string>(body), Is.EqualTo("hello"));
        }

        #endregion
    }
}
