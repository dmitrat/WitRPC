using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Common.Proxy.Utils;
using OutWit.Common.Reflection;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Interceptors
{
    public class RequestInterceptor : IProxyInterceptor
    {
        #region Constants

        private const string EVENT_SUBSCRIBE_PREFIX = "add_";
        private const string EVENT_UNSUBSCRIBE_PREFIX = "remove_";

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, Delegate> m_eventDelegates = new ();

        private readonly Dictionary<string, long> m_methodIds = new();

        private readonly long m_contractId;

        #endregion

        #region Constructors

        public RequestInterceptor(IClient client, bool strongAssemblyMatch, Type? contract = null)
        {
            Client = client;
            IsStrongAssemblyMatch = strongAssemblyMatch;

            if (contract != null)
                m_contractId = InitContract(contract);

            InitEvents();
        }

        #endregion

        #region Initialization

        private long InitContract(Type contract)
        {
            // Non-generic methods get precomputed ids so each call is one
            // dictionary lookup away from its id; generic methods keep the
            // name-based path (their closed signatures differ per call).
            foreach (var method in contract.GetAllMethods())
            {
                long methodId = ContractIds.GetMethodId(contract, method);
                if (methodId == ContractIds.NONE)
                    continue;

                m_methodIds[MethodKey(method.Name, method.GetParameters().Select(info => info.ParameterType))] = methodId;
            }

            return ContractIds.GetContractId(contract);
        }

        private void InitEvents()
        {
            Client.CallbackReceived += OnCallbackReceived;
        }

        private static string MethodKey(string methodName, IEnumerable<Type> parameterTypes)
        {
            return methodName + "(" + string.Join(",", parameterTypes.Select(ContractIds.StableName)) + ")";
        }

        #endregion

        #region IInterceptor

        public void Intercept(IProxyInvocation invocation)
        {
            if(invocation.MethodName.StartsWith(EVENT_SUBSCRIBE_PREFIX))
                SubscribeEvent(invocation);

            else if (invocation.MethodName.StartsWith(EVENT_UNSUBSCRIBE_PREFIX))
                UnsubscribeEvent(invocation);

            else if(invocation.ReturnsTask || invocation.ReturnsTaskWithResult)
                invocation.ReturnValue = InterceptMethodAsync(invocation);

            else 
                invocation.ReturnValue = InterceptMethod(invocation);

        }

        #endregion

        #region Process

        public async Task<object?> InterceptMethodAsync(IProxyInvocation invocation)
        {
            var parameterTypes = invocation.GetParametersTypes();
            var request = invocation.MethodName.CreateRequest(invocation.Parameters, parameterTypes, Client.ParametersSerializer);

            request.ContractId = m_contractId;

            if (m_contractId != ContractIds.NONE &&
                invocation.GetGenericArguments().Length == 0 &&
                m_methodIds.TryGetValue(MethodKey(invocation.MethodName, parameterTypes), out long methodId))
            {
                request.MethodId = methodId;
            }

            if (IsStrongAssemblyMatch)
            {
                request.ParameterTypes = parameterTypes;
                request.GenericArguments = invocation.GetGenericArguments();
            }
            else
            {
                request.ParameterTypesByName = parameterTypes.Select(type => new ParameterType(type)).ToArray();
                request.GenericArgumentsByName = invocation.GetGenericArguments().Select(type => new ParameterType(type)).ToArray();
            }

            var response = await Client.SendRequest(request);

            if (!response.IsSuccess())
                throw response.CreateFaultException();

            var returnType = invocation.GetReturnType();

            if (returnType == typeof(void) || invocation.ReturnsTask)
                return null;

            if (invocation.ReturnsTaskWithResult)
                returnType = returnType.GetGenericArguments()[0];

            try
            {
                if(response.Data == null || response.Data.Length == 0)
                    return null;
                
                return Client.ParametersSerializer.Deserialize(response.Data, returnType);
            }
            catch (Exception e)
            {
                throw response.CreateFaultException();
            }
        }

        public object? InterceptMethod(IProxyInvocation invocation)
        {
            return Task.Run(() => InterceptMethodAsync(invocation))
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        #endregion

        #region Functions
        
        private void SubscribeEvent(IProxyInvocation invocation)
        {
            var eventName = invocation.MethodName.Substring(EVENT_SUBSCRIBE_PREFIX.Length);
            var handler = (Delegate)invocation.Parameters[0];

            m_eventDelegates.AddOrUpdate(
                eventName,
                handler,
                (_, existing) => Delegate.Combine(existing, handler));
        }

        private void UnsubscribeEvent(IProxyInvocation invocation)
        {
            var eventName = invocation.MethodName.Substring(EVENT_UNSUBSCRIBE_PREFIX.Length);
            var handler = (Delegate)invocation.Parameters[0];

            if (!m_eventDelegates.TryGetValue(eventName, out Delegate? existing))
                return;

            var result = Delegate.Remove(existing, handler);
            if (result == null)
                m_eventDelegates.TryRemove(eventName, out Delegate? value);
            else
                m_eventDelegates[eventName] = result;
        }

        #endregion

        #region EventHandlers

        private void OnCallbackReceived(WitRequest? request)
        {
            if(request == null) 
                return;
            // A callback stamped for another contract is not this proxy's event,
            // even when the event names collide across services on one channel.
            if (request.ContractId != ContractIds.NONE &&
                m_contractId != ContractIds.NONE &&
                request.ContractId != m_contractId)
                return;

            if(!m_eventDelegates.TryGetValue(request.MethodName, out Delegate? handlers))
                return;

            handlers.DynamicInvoke(request.GetParameters(Client.ParametersSerializer));
        }

        #endregion

        #region Properties

        private bool IsStrongAssemblyMatch { get; }

        private IClient Client { get; }

        #endregion
    }
}
