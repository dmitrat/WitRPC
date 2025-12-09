using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OutWit.Common.Reflection;
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
            foreach (EventInfo info in typeof(TService).GetAllEvents())
                info.AddEventHandler(Service,  info.CreateUniversalHandler(this, HandleEvent));
        }

        #endregion

        #region IProcessor

        public void ResetSerializer(IMessageSerializer serializer)
        {
            Serializer = serializer;
        }

        public async Task<WitResponse> Process(WitRequest? request)
        {
            if(Serializer == null)
                return WitResponse.InternalServerError("Serializer is missing");
            
            if (request == null)
                return WitResponse.BadRequest("Request is empty");

            var method = request.GetMethod(Service);
            if(method == null)
                return WitResponse.BadRequest($"Method not found on service, method name: {request.MethodName}");

            try
            {
                var returnType = method.ReturnType;
                if (returnType == typeof(Task))
                    return await ProcessAsync(method, request.GetParameters(Serializer));
                
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    return await ProcessGenericAsync(method, request.GetParameters(Serializer));
                else 
                    return method.Invoke(Service, request.GetParameters(Serializer)).Success(Serializer);
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to process request", e);
            }
            
        }

        private async Task<WitResponse> ProcessAsync(MethodInfo method, object?[] parameters)
        {
            try
            {
                var task = method.Invoke(Service, parameters) as Task;
                if (task == null)
                    return WitResponse.InternalServerError("Failed to process request");

                await task;

                return WitResponse.Success(null);
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to process request", e);
            }
        }

        private async Task<WitResponse> ProcessGenericAsync(MethodInfo method, object?[] parameters)
        {
            try
            {
                if(Serializer == null)
                    return WitResponse.InternalServerError("Serializer is missing");

                var task = method.Invoke(Service, parameters) as Task;
                if (task == null)
                    return WitResponse.InternalServerError("Failed to process request");

                object? result = await task.ContinueWith(t => (object)((dynamic)t).Result);

                return result.Success(Serializer);
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to process request", e);
            }
        }

        #endregion

        #region Functions

        private void RaiseCallback(WitRequest? request)
        {
            Callback(request);
        }

        #endregion

        #region Static

        private static void HandleEvent(RequestProcessor<TService> sender, string eventName, object[] parameters)
        {
            if(sender.Serializer == null)
                return;

            var parameterTypes = parameters.Select(x => x?.GetType() ?? typeof(object)).ToArray();
            var request = eventName.CreateRequest(parameters, parameterTypes, sender.Serializer);

            if (sender.IsStrongAssemblyMatch)
            {
                request.ParameterTypes = parameterTypes;
                request.GenericArguments = Array.Empty<Type>();
            }
            else
            {
                request.ParameterTypesByName = parameterTypes.Select(x => (ParameterType)x).ToArray();
                request.GenericArgumentsByName = Array.Empty<ParameterType>();
            }


            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter is TService)
                    request.Parameters[i] = Array.Empty<byte>();
            }

            sender.RaiseCallback(request);
        }

        #endregion

        #region Properties

        private TService Service { get; }

        private bool IsStrongAssemblyMatch { get; }
        
        private IMessageSerializer? Serializer { get; set; }

        #endregion
    }
}
