using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using System.Reflection;
using System.Threading.Tasks;
using OutWit.Common.Reflection;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Rest.Processors
{
    public class RequestProcessorRest<TService> : IRequestProcessor
        where TService : class
    {
        #region Events

        public event RequestProcessorEventHandler Callback = delegate { };

        #endregion

        #region Constructors

        public RequestProcessorRest(TService service)
        {
            Service = service;
            //ValueConverter = new ValueConverterJson();
        }

        #endregion

        #region Initialization

        #endregion

        #region IProcessor
        
        public void ResetSerializer(IMessageSerializer serializer)
        {
            Serializer = serializer;
        }
        
        public async Task<WitResponse> Process(WitRequest? request)
        {
            if(Serializer == null)
                return WitResponse.InternalServerError("Serializer is empty");

            if (request == null)
                return WitResponse.BadRequest("Request is empty");

            var method = GetMethod(request, Service, out List<object?>? parameters);
            if (method == null)
                return WitResponse.BadRequest($"Method not found on service, method name: {request.MethodName}");

            try
            {
                var returnType = method.ReturnType;
                if (returnType == typeof(Task))
                    return await ProcessAsync(method, parameters?.ToArray());
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    return await ProcessGenericAsync(method, request.GetParameters(Serializer));
                else
                    return method.Invoke(Service, parameters?.ToArray()).Success(Serializer);
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to process request", e);
            }

        }

        private async Task<WitResponse> ProcessAsync(MethodInfo method, object?[]? parameters)
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

        private async Task<WitResponse> ProcessGenericAsync(MethodInfo method, object?[]? parameters)
        {
            try
            {
                if (Serializer == null)
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

        public MethodInfo? GetMethod(WitRequest? me, TService service, out List<object?>? parameters)
        {
            parameters = null;

            if (me == null || string.IsNullOrEmpty(me.MethodName))
                return null;

            try
            {
                IReadOnlyList<MethodInfo> candidates = typeof(TService)
                    .GetAllMethods()
                    .Where(info => info.Name == me.MethodName)
                    .ToList();

                foreach (MethodInfo method in candidates)
                {
                    if(method.IsGenericMethod)
                        continue;

                    IReadOnlyList<ParameterInfo> candidateParameters = method.GetParameters();

                    if (candidateParameters.Count != me.Parameters.Length)
                        continue;

                    if(TryRestoreParameters(me.Parameters, candidateParameters, out parameters))
                        return method;
                }

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private bool TryRestoreParameters(IList<byte[]> sourceParameters, IReadOnlyList<ParameterInfo> candidateParameters, out List<object?> parameters)
        {
            parameters = new List<object?>();

            if (Serializer == null)
                return false;

            for (int i = 0; i < candidateParameters.Count; i++)
            {
                var type = candidateParameters[i].ParameterType;
                var origValue = sourceParameters[i];

                parameters.Add(Serializer.Deserialize(origValue, type));
            }

            return true;
        }

        #endregion

        #region Properties

        private TService Service { get; }
        
        private IMessageSerializer? Serializer { get; set; }

        #endregion
    }
}
