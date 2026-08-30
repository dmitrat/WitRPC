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
        private readonly Dictionary<string, long> m_eventContractIds = new();

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

            // A service registered as its CLASS is still called through its contracts: a
            // proxy built on an implemented interface stamps that interface's method ids,
            // which the class-scoped table above cannot know. Index those ids too, so such
            // a call takes the id path (declared parameter types, one lookup) instead of
            // falling back to the name scan. An interface MethodInfo invokes virtually on
            // the instance, so it is the right handle to keep.
            if (typeof(TService).IsInterface)
                return;

            foreach (Type contract in typeof(TService).GetInterfaces())
            {
                foreach (MethodInfo method in contract.GetAllMethods())
                {
                    long methodId = ContractIds.GetMethodId(contract, method);
                    if (methodId != ContractIds.NONE)
                        m_methodsById.TryAdd(methodId, method);
                }
            }
        }

        private void InitEvents()
        {
            foreach (EventInfo info in typeof(TService).GetAllEvents())
            {
                info.AddEventHandler(Service,  info.CreateUniversalHandler(this, HandleEvent));
                m_eventContractIds[info.Name] = GetEventContractId(info);
            }
        }

        /// <summary>
        /// The contract a callback for <paramref name="info"/> is stamped with. A proxy on
        /// the client side is built on an interface and drops callbacks stamped for any other
        /// contract, so an event a class inherits from one of its interfaces travels under
        /// that interface's id -- registering the service as the class instead of the contract
        /// used to silence every event it raised. An event the class alone declares keeps the
        /// class as its contract.
        /// </summary>
        private long GetEventContractId(EventInfo info)
        {
            if (typeof(TService).IsInterface)
                return m_contractId;

            foreach (Type contract in typeof(TService).GetInterfaces())
            {
                EventInfo? declared = contract.GetEvent(info.Name);
                if (declared != null && declared.EventHandlerType == info.EventHandlerType)
                    return ContractIds.GetContractId(contract);
            }

            return m_contractId;
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
            // event names collide across services -- the contract that declares
            // the event, not necessarily the type the service was registered as.
            request.ContractId = sender.m_eventContractIds.TryGetValue(eventName, out long contractId)
                ? contractId
                : sender.m_contractId;

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
