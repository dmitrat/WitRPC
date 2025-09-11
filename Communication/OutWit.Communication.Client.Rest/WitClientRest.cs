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
    public class WitClientRest : IClient
    {
        #region Event Handler

        public event ClientEventHandler CallbackReceived = delegate { };

        #endregion

        #region Constructors

        public WitClientRest(RestClientTransportOptions options, IAccessTokenProvider tokenProvider)
        {
            if (string.IsNullOrEmpty(options.Host?.Connection))
                throw new WitException("Url cannot be empty");

            Options = options;
            ParametersSerializer = new MessageSerializerJson();
            TokenProvider = tokenProvider;
            //Converter = new ValueConverterJson();

            Client = RestClientBuilder.Create();
        }

        #endregion

        public async Task<WitResponse> SendRequest(WitRequest? request)
        {
            if(request == null)
                return WitResponse.BadRequest("Empty request");

            IRequestMessage? requestMessage = await request.ConstructGetRequest(Options, TokenProvider) ??
                                              await request.ConstructPostRequest(Options, TokenProvider);

            if(requestMessage == null)
                return WitResponse.BadRequest("Cannot construct request");

            return await Client.SendAsync<WitResponse>(requestMessage);
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
