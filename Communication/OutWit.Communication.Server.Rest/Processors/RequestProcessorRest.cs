using System;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;
using System.Reflection;
using OutWit.Communication.Converters;

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
            ValueConverter = new ValueConverterJson();
        }

        #endregion

        #region Initialization

        #endregion

        #region IProcessor

        public WitComResponse Process(WitComRequest? request)
        {
            if (request == null)
                return WitComResponse.BadRequest("Request is empty");

            var method = GetMethod(request, Service, out List<object?>? parameters);
            if (method == null)
                return WitComResponse.BadRequest($"Method not found on service, method name: {request.MethodName}");

            try
            {
                return WitComResponse.Success(method.Invoke(Service, parameters?.ToArray()));
            }
            catch (Exception e)
            {
                return WitComResponse.InternalServerError("Failed to process request", e);
            }

        }

        #endregion

        #region Functions

        public MethodInfo? GetMethod(WitComRequest? me, TService service, out List<object?>? parameters)
        {
            parameters = null;

            if (me == null || string.IsNullOrEmpty(me.MethodName))
                return null;

            try
            {
                IReadOnlyList<MethodInfo> candidates = typeof(TService)
                    .GetMethods()
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

        private bool TryRestoreParameters(IReadOnlyList<object> sourceParameters, IReadOnlyList<ParameterInfo> candidateParameters, out List<object?> parameters)
        {
            parameters = new List<object?>();

            for (int i = 0; i < candidateParameters.Count; i++)
            {
                var type = candidateParameters[i].ParameterType;
                var origValue = sourceParameters[i];

                if(!ValueConverter.TryConvert(origValue, type, out object? destValue))
                    return false;

                parameters.Add(destValue);
            }

            return true;
        }

        #endregion

        #region Static

        #endregion

        #region Properties

        private TService Service { get; }

        private IValueConverter ValueConverter { get; }

        #endregion
    }
}
