using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Model;
using OutWit.Communication.Requests;

namespace OutWit.Communication.Interceptors
{
    public class RequestInterceptor : IInterceptor
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

        public void Intercept(IInvocation invocation)
        {
            if(invocation.Method.Name.StartsWith(EVENT_SUBSCRIBE_PREFIX))
                SubscribeEvent(invocation);

            else if (invocation.Method.Name.StartsWith(EVENT_UNSUBSCRIBE_PREFIX))
                UnsubscribeEvent(invocation);

            else
                ProcessRequest(invocation).Wait();
            
        }

        #endregion

        #region Functions

        private async Task ProcessRequest(IInvocation invocation)
        {
            var request = new WitComRequest
            {
                MethodName = invocation.Method.Name,
                Parameters = invocation.Arguments,
            };

            if (IsStrongAssemblyMatch)
            {
                request.ParameterTypes = invocation.Method.GetParameters().Select(info => info.ParameterType).ToArray();
                request.GenericArguments = invocation.Method.GetGenericArguments();
            }
            else
            {
                request.ParameterTypesByName = invocation.Method.GetParameters().Select(info => new ParameterType(info.ParameterType)).ToArray();
                request.GenericArgumentsByName = invocation.Method.GetGenericArguments().Select(type => new ParameterType(type)).ToArray();
            }

            var response = await Client.SendRequest(request);

            if (!response.IsSuccess())
                throw response.CreateFaultException();

            if(invocation.Method.ReturnType == typeof(void))
                return;

            if(!Client.Converter.TryConvert(response.Data, invocation.Method.ReturnType, out var value))
                throw response.CreateFaultException();
            
            invocation.ReturnValue = value;
        }

        private void SubscribeEvent(IInvocation invocation)
        {
            var eventName = invocation.Method.Name.Substring(EVENT_SUBSCRIBE_PREFIX.Length);
            var handler = (Delegate)invocation.Arguments[0];

            if (m_eventDelegates.TryGetValue(eventName, out Delegate? existing))
                m_eventDelegates[eventName] = Delegate.Combine(existing, handler);
            else
                m_eventDelegates.TryAdd(eventName, handler);
        }

        private void UnsubscribeEvent(IInvocation invocation)
        {
            var eventName = invocation.Method.Name.Substring(EVENT_UNSUBSCRIBE_PREFIX.Length);
            var handler = (Delegate)invocation.Arguments[0];

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
