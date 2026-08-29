using System;
using System.Collections.Generic;
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

        #region Fields

        private readonly Dictionary<long, MethodInfo> m_methodsById = new();

        private readonly long m_contractId;

        #endregion

        #region Constructors

        public RequestProcessor(TService service, bool isStrongAssemblyMatch = true)
        {
            Service = service;
            IsStrongAssemblyMatch = isStrongAssemblyMatch;

            m_contractId = ContractIds.GetContractId(typeof(TService));
            InitMethods();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitMethods()
        {
            foreach (MethodInfo method in typeof(TService).GetAllMethods())
            {
                long methodId = ContractIds.GetMethodId(typeof(TService), method);
                if (methodId == ContractIds.NONE)
                    continue;

                if (!m_methodsById.TryAdd(methodId, method))
                    throw new InvalidOperationException(
                        $"Method id collision on {typeof(TService).Name}: '{method.Name}' clashes with '{m_methodsById[methodId].Name}'");
            }
        }

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

            MethodInfo? method = null;
            object?[]? parameters = null;

            // The id path: one dictionary lookup, parameters deserialized
            // against the method's declared types -- no reflection scan, no
            // type-name resolution from the wire.
            if (request.MethodId != ContractIds.NONE &&
                m_methodsById.TryGetValue(request.MethodId, out MethodInfo? byId) &&
                byId.GetParameters().Length == request.Parameters.Length)
            {
                method = byId;
                parameters = request.GetParameters(Serializer,
                    byId.GetParameters().Select(info => info.ParameterType).ToArray());
            }

            if (method == null)
            {
                method = request.GetMethod(Service);
                if (method == null)
                    return WitResponse.BadRequest($"Method not found on service, method name: {request.MethodName}");

                parameters = request.GetParameters(Serializer);
            }

            try
            {
                var returnType = method.ReturnType;
                if (returnType == typeof(Task))
                    return await ProcessAsync(method, parameters!);
                
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    return await ProcessGenericAsync(method, parameters!);
                else 
                    return method.Invoke(Service, parameters).Success(Serializer);
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

            // The callback names its contract so a client holding several
            // proxies on one channel delivers it to the right one even when
            // event names collide across services.
            request.ContractId = sender.m_contractId;

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
