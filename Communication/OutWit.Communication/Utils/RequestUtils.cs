using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using OutWit.Common.Reflection;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Utils
{
    public static class RequestUtils
    {
        public static WitResponse GetResponse(this byte[]? me, IMessageSerializer serializer)
        {
            if (me == null)
                return WitResponse.InternalServerError("Server return empty response");

            try
            {
                var response = serializer.Deserialize<WitResponse>(me);
                if (response == null)
                    return WitResponse.InternalServerError("Failed to deserialize response");

                return response;
            }
            catch (Exception e)
            {
                return WitResponse.InternalServerError("Failed to deserialize response", e);
            }
        }

        public static WitRequest CreateRequest(this string me, IReadOnlyList<object> parameters,
            IMessageSerializer serializer, ILogger? logger = null)
        {
            return new WitRequest
            {
                MethodName = me,
                Parameters = parameters.Select(parameter => serializer.Serialize(parameter, parameter.GetType(), logger)).ToArray()
            };
        }

        public static object?[] GetParameters(this WitRequest me, IMessageSerializer serializer, ILogger? logger = null)
        {
            if (me.Parameters.Length == 0)
                return Array.Empty<object>();

            IList<byte[]> bytes = me.Parameters;
            IReadOnlyList<Type> types;

            if (me.ParameterTypes.Length > 0)
                types = me.ParameterTypes;

            else if(me.ParameterTypesByName.Length > 0)
                types = me.ParameterTypesByName.Select(type => (Type)type!).ToList();
            else
                throw new WitExceptionSerialization("Cannot deserialize request: parameter types missing");

            var result = new List<object?>();

            for (int i = 0; i < Math.Min(bytes.Count, types.Count); i++)
                result.Add(serializer.Deserialize(bytes[i], types[i]));

            return result.ToArray();
        }

        public static WitRequest? GetRequest(this byte[]? data, IMessageSerializer serializer)
        {
            if (data == null)
                return null;

            try
            {
                var request = serializer.Deserialize<WitRequest>(data);
                if (request == null)
                    return null;

                //for (int i = 0; i < request.Parameters.Count; i++)
                //{
                //    var parameter = request.Parameters[i];

                //    Type parameterType;
                //    if (request.ParameterTypes.Count > 0)
                //        parameterType = request.ParameterTypes[i];
                //    else if (request.ParameterTypesByName.Count > 0)
                //        parameterType = (Type)request.ParameterTypesByName[i]!;
                //    else
                //        throw new WitExceptionSerialization("Cannot deserialize request: parameter types missing");

                //    if (valueConverter.TryConvert(parameter, parameterType, out object? value) && value != null)
                //        request.Parameters[i] = value;

                //}

                return request;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public static MethodInfo? GetMethod<TService>(this WitRequest? me, TService service)
            where TService : class
        {
            if (me == null || string.IsNullOrEmpty(me.MethodName))
                return null;

            try
            {
                IReadOnlyList<MethodInfo> candidates = typeof(TService)
                    .GetAllMethods()
                    .Where(info => info.Name.Equals(me.MethodName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                var parameterTypes = me.ParameterTypes.ToArray();
                if (me.ParameterTypesByName.Length > 0)
                    parameterTypes = me.ParameterTypesByName.Select(type => (Type)type!).ToArray();

                var genericArguments = me.GenericArguments.ToArray();
                if (me.GenericArgumentsByName.Length > 0)
                    genericArguments = me.GenericArgumentsByName.Select(type => (Type)type!).ToArray();

                foreach (MethodInfo method in candidates)
                {
                    IReadOnlyList<Type> candidateParameters =
                        method
                            .GetParameters()
                            .Select(info => info.ParameterType)
                            .CheckGenericParameters(genericArguments);

                    if (candidateParameters.Is(parameterTypes))
                        return method.IsGenericMethod ? method.MakeGenericMethod(genericArguments) : method;
                }

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static IReadOnlyList<Type> CheckGenericParameters(this IEnumerable<Type> me, IReadOnlyList<Type> genericArguments)
        {
            Type[] types = me.ToArray();
            if (genericArguments.Count == 0)
                return types;

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];

                if (type.IsGenericParameter && type.GenericParameterPosition < genericArguments.Count)
                    types[i] = genericArguments[type.GenericParameterPosition];

                else if (type.IsGenericType)
                    types[i] = type.CheckGenericType(genericArguments);
                
            }

            return types;

        }

        private static Type CheckGenericType(this Type me, IReadOnlyList<Type> genericArguments)
        {
            if (!me.IsGenericType)
                return me;

            var typeArguments = new Type[me.GenericTypeArguments.Length];

            for(int i = 0; i < typeArguments.Length; i++)
            {
                var argument = me.GenericTypeArguments[i];
                if(argument.IsGenericType)
                    typeArguments[i] = argument.CheckGenericType(genericArguments);
                else if(argument.GenericParameterPosition < genericArguments.Count)
                    typeArguments[i] = genericArguments[argument.GenericParameterPosition];
            }

            return me.GetGenericTypeDefinition().MakeGenericType(typeArguments);
        }

        private static bool Is(this IReadOnlyList<Type> me, IReadOnlyList<Type> candidates)
        {
            if(me.Count != candidates.Count)
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
