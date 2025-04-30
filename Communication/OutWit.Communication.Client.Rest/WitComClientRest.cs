using System.Threading.Tasks;
using OutWit.Common.Rest;
using OutWit.Common.Rest.Interfaces;
using OutWit.Communication.Client.Rest.Utils;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Serializers;

namespace OutWit.Communication.Client.Rest
{
    public class WitComClientRest : IClient
    {
        #region Event Handler

        public event ClientEventHandler CallbackReceived = delegate { };

        #endregion

        #region Constructors

        public WitComClientRest(RestClientTransportOptions options, IAccessTokenProvider tokenProvider)
        {
            if (string.IsNullOrEmpty(options.Host?.Connection))
                throw new WitComException("Url cannot be empty");

            Options = options;
            ParametersSerializer = new MessageSerializerJson();
            TokenProvider = tokenProvider;
            //Converter = new ValueConverterJson();

            Client = RestClientBuilder.Create();
        }

        #endregion

        public async Task<WitComResponse> SendRequest(WitComRequest? request)
        {
            if(request == null)
                return WitComResponse.BadRequest("Empty request");

            IRequestMessage? requestMessage = request.ConstructGetRequest(Options, TokenProvider) ??
                                              request.ConstructPostRequest(Options, TokenProvider);

            if(requestMessage == null)
                return WitComResponse.BadRequest("Cannot construct request");

            return await Client.SendAsync<WitComResponse>(requestMessage);
        }

        #region Properties

        public RestClientBase Client { get; }

        #endregion

        #region Services

        public RestClientTransportOptions Options { get; }

        public IMessageSerializer ParametersSerializer { get; }

        public IAccessTokenProvider TokenProvider { get; }


        #endregion
    }
}
