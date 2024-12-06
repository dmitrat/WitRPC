using System;
using System.Reflection;
using System.Runtime.Serialization;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Requests;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Utils
{
    public static class RequestUtils
    {
        public static WitComResponse GetResponse(this byte[]? me, IMessageSerializer serializer)
        {
            if (me == null)
                return WitComResponse.InternalServerError("Server return empty response");

            try
            {
                var response = serializer.Deserialize<WitComResponse>(me);
                if (response == null)
                    return WitComResponse.InternalServerError("Failed to deserialize response");

                return WitComResponse.Success(response.Data);
            }
            catch (Exception e)
            {
                return WitComResponse.InternalServerError("Failed to deserialize response", e);
            }
        }

        public static WitComRequest? GetRequest(this byte[]? data, IMessageSerializer serializer, IValueConverter valueConverter)
        {
            if (data == null)
                return null;

            try
            {
                var request = serializer.Deserialize<WitComRequest>(data);
                if (request == null)
                    return null;

                for (int i = 0; i < request.Parameters.Length; i++)
                {
                    var parameter = request.Parameters[i];

                    Type parameterType;
                    if (request.ParameterTypes.Count > 0)
                        parameterType = request.ParameterTypes[i];
                    else if (request.ParameterTypesByName.Count > 0)
                        parameterType = (Type)request.ParameterTypesByName[i]!;
                    else
                        throw new SerializationException("Cannot deserialize request: parameter types missing");

                    if (valueConverter.TryConvert(parameter, parameterType, out object? value) && value != null)
                        request.Parameters[i] = value;

                }

                return request;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public static MethodInfo? GetMethod<TService>(this WitComRequest? me, TService service)
            where TService : class
        {
            if (me == null || string.IsNullOrEmpty(me.MethodName))
                return null;

            try
            {
                IReadOnlyList<MethodInfo> candidates = typeof(TService)
                    .GetMethods()
                    .Where(info => info.Name == me.MethodName)
                    .ToList();

                var parameterTypes = me.ParameterTypes.ToArray();
                if (me.ParameterTypesByName.Count > 0)
                    parameterTypes = me.ParameterTypesByName.Select(type => (Type)type!).ToArray();

                var genericArguments = me.GenericArguments.ToArray();
                if (me.GenericArgumentsByName.Count > 0)
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
