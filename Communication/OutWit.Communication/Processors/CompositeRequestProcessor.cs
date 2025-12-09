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
    /// <summary>
    /// A request processor that can handle multiple service interfaces.
    /// Routes requests to the appropriate service based on the method being called.
    /// </summary>
    public class CompositeRequestProcessor : IRequestProcessor
    {
        #region Events

        public event RequestProcessorEventHandler Callback = delegate { };

        #endregion

        #region Constructors

        public CompositeRequestProcessor(bool isStrongAssemblyMatch = true)
        {
            IsStrongAssemblyMatch = isStrongAssemblyMatch;
            Services = new Dictionary<Type, ServiceInfo>();
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a service with its interface type.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="service">The service implementation.</param>
        /// <returns>This processor for chaining.</returns>
        public CompositeRequestProcessor Register<TService>(TService service)
            where TService : class
        {
            var serviceType = typeof(TService);
            
            if (Services.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered.");

            var serviceInfo = new ServiceInfo(serviceType, service);
            Services[serviceType] = serviceInfo;

            // Subscribe to events
            foreach (EventInfo info in serviceType.GetAllEvents())
                info.AddEventHandler(service, info.CreateUniversalHandler(this, HandleEvent));

            return this;
        }

        /// <summary>
        /// Registers a service with its interface type.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="service">The service implementation.</param>
        /// <returns>This processor for chaining.</returns>
        public CompositeRequestProcessor Register<TInterface, TImplementation>(TImplementation service)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            return Register<TInterface>(service);
        }

        #endregion

        #region IRequestProcessor

        public void ResetSerializer(IMessageSerializer serializer)
        {
            Serializer = serializer;
        }

        public async Task<WitResponse> Process(WitRequest? request)
        {
            if (Serializer == null)
                return WitResponse.InternalServerError("Serializer is missing");

            if (request == null)
                return WitResponse.BadRequest("Request is empty");

            // Find the service that can handle this request
            var (serviceInfo, method) = FindServiceAndMethod(request);
            
            if (serviceInfo == null || method == null)
                return WitResponse.BadRequest($"Method not found on any registered service, method name: {request.MethodName}");

            try
            {
                var returnType = method.ReturnType;
                var parameters = request.GetParameters(Serializer);

                if (returnType == typeof(Task))
                    return await ProcessAsync(serviceInfo.Service, method, parameters);

                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    return await ProcessGenericAsync(serviceInfo.Service, method, parameters);
                
                return method.Invoke(serviceInfo.Service, parameters).Success(Serializer);
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to process request", e);
            }
        }

        #endregion

        #region Private Methods

        private (ServiceInfo? serviceInfo, MethodInfo? method) FindServiceAndMethod(WitRequest request)
        {
            foreach (var kvp in Services)
            {
                var method = GetMethodFromService(request, kvp.Value);
                if (method != null)
                    return (kvp.Value, method);
            }

            return (null, null);
        }

        private MethodInfo? GetMethodFromService(WitRequest request, ServiceInfo serviceInfo)
        {
            if (string.IsNullOrEmpty(request.MethodName))
                return null;

            try
            {
                IReadOnlyList<MethodInfo> candidates = serviceInfo.ServiceType
                    .GetAllMethods()
                    .Where(info => info.Name.Equals(request.MethodName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (candidates.Count == 0)
                    return null;

                var parameterTypes = request.ParameterTypes.ToArray();
                if (request.ParameterTypesByName.Length > 0)
                    parameterTypes = request.ParameterTypesByName.Select(type => (Type)type!).ToArray();

                var genericArguments = request.GenericArguments.ToArray();
                if (request.GenericArgumentsByName.Length > 0)
                    genericArguments = request.GenericArgumentsByName.Select(type => (Type)type!).ToArray();

                foreach (MethodInfo method in candidates)
                {
                    IReadOnlyList<Type> candidateParameters =
                        method
                            .GetParameters()
                            .Select(info => info.ParameterType)
                            .ToList()
                            .CheckGenericParameters(genericArguments);

                    if (candidateParameters.Is(parameterTypes))
                        return method.IsGenericMethod ? method.MakeGenericMethod(genericArguments) : method;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<WitResponse> ProcessAsync(object service, MethodInfo method, object?[] parameters)
        {
            try
            {
                var task = method.Invoke(service, parameters) as Task;
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

        private async Task<WitResponse> ProcessGenericAsync(object service, MethodInfo method, object?[] parameters)
        {
            try
            {
                if (Serializer == null)
                    return WitResponse.InternalServerError("Serializer is missing");

                var task = method.Invoke(service, parameters) as Task;
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

        private void RaiseCallback(WitRequest? request)
        {
            Callback(request);
        }

        #endregion

        #region Static Event Handler

        private static void HandleEvent(CompositeRequestProcessor sender, string eventName, object[] parameters)
        {
            if (sender.Serializer == null)
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

            // Clear service reference parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter != null && sender.Services.Values.Any(s => s.Service == parameter))
                    request.Parameters[i] = Array.Empty<byte>();
            }

            sender.RaiseCallback(request);
        }

        #endregion

        #region Properties

        private Dictionary<Type, ServiceInfo> Services { get; }

        private bool IsStrongAssemblyMatch { get; }

        private IMessageSerializer? Serializer { get; set; }

        #endregion

        #region Nested Types

        private class ServiceInfo
        {
            public ServiceInfo(Type serviceType, object service)
            {
                ServiceType = serviceType;
                Service = service;
            }

            public Type ServiceType { get; }
            public object Service { get; }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for CompositeRequestProcessor helper operations.
    /// </summary>
    internal static class CompositeRequestProcessorExtensions
    {
        public static IReadOnlyList<Type> CheckGenericParameters(this IReadOnlyList<Type> types, IReadOnlyList<Type> genericArguments)
        {
            var result = types.ToArray();
            if (genericArguments.Count == 0)
                return result;

            for (int i = 0; i < result.Length; i++)
            {
                var type = result[i];

                if (type.IsGenericParameter && type.GenericParameterPosition < genericArguments.Count)
                    result[i] = genericArguments[type.GenericParameterPosition];
                else if (type.IsGenericType)
                    result[i] = type.CheckGenericType(genericArguments);
            }

            return result;
        }

        public static Type CheckGenericType(this Type type, IReadOnlyList<Type> genericArguments)
        {
            if (!type.IsGenericType)
                return type;

            var typeArguments = new Type[type.GenericTypeArguments.Length];

            for (int i = 0; i < typeArguments.Length; i++)
            {
                var argument = type.GenericTypeArguments[i];
                if (argument.IsGenericType)
                    typeArguments[i] = argument.CheckGenericType(genericArguments);
                else if (argument.GenericParameterPosition < genericArguments.Count)
                    typeArguments[i] = genericArguments[argument.GenericParameterPosition];
            }

            return type.GetGenericTypeDefinition().MakeGenericType(typeArguments);
        }

        public static bool Is(this IReadOnlyList<Type> me, IReadOnlyList<Type> candidates)
        {
            if (me.Count != candidates.Count)
                return false;

            for (int i = 0; i < me.Count; i++)
            {
                if (me[i] != candidates[i])
                    return false;
            }

            return true;
        }
    }
}
