using System;
using System.Reflection;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Processors
{
    public class RequestProcessor<TService> : IRequestProcessor
        where TService : class
    {
        #region Events

        public event RequestProcessorEventHandler Callback = delegate { };

        #endregion

        #region Constructors

        public RequestProcessor(TService service, bool isStrongAssemblyMatch = true)
        {
            Service = service;
            IsStrongAssemblyMatch = isStrongAssemblyMatch;

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            foreach (EventInfo info in typeof(TService).GetEvents())
                info.AddEventHandler(Service,  info.CreateUniversalHandler(this, HandleEvent));
        }

        #endregion

        #region IProcessor

        public WitComResponse Process(WitComRequest? request)
        {
            if(request == null)
                return WitComResponse.BadRequest("Request is empty");

            var method = request.GetMethod(Service);
            if(method == null)
                return WitComResponse.BadRequest($"Method not found on service, method name: {request.MethodName}");

            try
            {
                return WitComResponse.Success(method.Invoke(Service, request.Parameters));
            }
            catch (Exception e)
            {
                return WitComResponse.InternalServerError("Failed to process request", e);
            }
            
        }

        #endregion

        #region Functions

        private void RaiseCallback(WitComRequest? request)
        {
            Callback(request);
        }

        #endregion

        #region Static

        private static void HandleEvent(RequestProcessor<TService> sender, string eventName, object[] parameters)
        {
            var request = new WitComRequest
            {
                MethodName = eventName,
                Parameters = parameters
            };

            if (sender.IsStrongAssemblyMatch)
            {
                request.ParameterTypes = parameters.Select(x => x.GetType()).ToArray();
                request.GenericArguments = Array.Empty<Type>();
            }
            else
            {
                request.ParameterTypesByName = parameters.Select(x => (ParameterType)x.GetType()).ToArray();
                request.GenericArgumentsByName = Array.Empty<ParameterType>();
            }


            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter is TService)
                    request.Parameters[i] = null;
            }

            sender.RaiseCallback(request);
        }

        #endregion

        #region Properties

        private TService Service { get; }

        private bool IsStrongAssemblyMatch { get; }

        #endregion
    }
}
