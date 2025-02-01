using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Common.Proxy.Utils;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;

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

        #endregion

        #region Constructors

        public RequestInterceptor(IClient client, bool strongAssemblyMatch)
        {
            Client = client;
            IsStrongAssemblyMatch = strongAssemblyMatch;

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            Client.CallbackReceived += OnCallbackReceived;
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
            var request = new WitComRequest
            {
                MethodName = invocation.MethodName,
                Parameters = invocation.Parameters,
            };

            if (IsStrongAssemblyMatch)
            {
                request.ParameterTypes = invocation.GetParametersTypes();
                request.GenericArguments = invocation.GetGenericArguments();
            }
            else
            {
                request.ParameterTypesByName = invocation.GetParametersTypes().Select(type => new ParameterType(type)).ToArray();
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

            if (!Client.Converter.TryConvert(response.Data, returnType, out var value))
                throw response.CreateFaultException();

            return value;
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

            if (m_eventDelegates.TryGetValue(eventName, out Delegate? existing))
                m_eventDelegates[eventName] = Delegate.Combine(existing, handler);
            else
                m_eventDelegates.TryAdd(eventName, handler);
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

        private void OnCallbackReceived(WitComRequest? request)
        {
            if(request == null) 
                return;
            if(!m_eventDelegates.TryGetValue(request.MethodName, out Delegate? handlers))
                return;

            handlers.DynamicInvoke(request.Parameters);
        }

        #endregion

        #region Properties

        private bool IsStrongAssemblyMatch { get; }

        private IClient Client { get; }

        #endregion
    }
}
