using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using OutWit.Common.Reflection;
using OutWit.Communication.Requests;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Server.Rest
{
    /// <summary>
    /// The readable side of the REST transport: knows the service contracts by
    /// reflection and turns a plain HTTP call -- a JSON object of named
    /// arguments, a JSON array of positional ones, or a query string -- into the
    /// <see cref="WitRequest"/> the shared request processor dispatches. Every
    /// argument is bound against the method's declared parameter type, so a
    /// caller writes <c>{"message":"hello","count":3}</c> and nothing else: no
    /// envelope, no type names, no encoding.
    /// </summary>
    public sealed class RestMethodCatalog
    {
        #region Constants

        private const string POSITIONAL_PREFIX = "param";

        #endregion

        #region Fields

        private readonly Dictionary<string, List<RestMethod>> m_methods = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructors

        public RestMethodCatalog(IEnumerable<Type> contracts)
        {
            foreach (var contract in contracts)
                InitContract(contract);
        }

        #endregion

        #region Initialization

        private void InitContract(Type contract)
        {
            long contractId = ContractIds.GetContractId(contract);

            foreach (MethodInfo method in contract.GetAllMethods())
            {
                // Generic methods cannot be bound from JSON, and event accessors are
                // not callable over a stateless transport; property accessors are.
                if (method.IsGenericMethod || IsEventAccessor(method))
                    continue;

                if (!m_methods.TryGetValue(method.Name, out var overloads))
                {
                    overloads = new List<RestMethod>();
                    m_methods[method.Name] = overloads;
                }

                overloads.Add(new RestMethod(contract, contractId, method, ContractIds.GetMethodId(contract, method)));
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Binds a JSON body -- an object of named arguments or an array of
        /// positional ones -- to a method. A <c>null</c> body means no arguments.
        /// </summary>
        /// <param name="methodName">The last URL segment.</param>
        /// <param name="body">The parsed JSON body, or <c>null</c> for an empty body.</param>
        /// <returns>The bound request, or the HTTP status and reason the caller gets instead.</returns>
        public RestBinding BindBody(string methodName, JsonElement? body)
        {
            if (body == null)
                return Bind(methodName, Array.Empty<RestArgument>());

            var element = body.Value;

            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    return Bind(methodName, element.EnumerateArray().Select(item => new RestArgument(null, item)).ToArray());

                case JsonValueKind.Object:
                    return Bind(methodName, element.EnumerateObject().Select(property => new RestArgument(property.Name, property.Value)).ToArray());

                default:
                    return RestBinding.Fail(HttpStatusCode.BadRequest, "The body must be a JSON object of named arguments or a JSON array of positional ones");
            }
        }

        /// <summary>
        /// Binds a query string to a method. Values are plain text and are
        /// interpreted against the declared parameter type: strings, enums, GUIDs
        /// and dates are taken verbatim, numbers and booleans must parse, and
        /// anything else must be JSON.
        /// </summary>
        /// <param name="methodName">The last URL segment.</param>
        /// <param name="query">The parsed query string.</param>
        /// <returns>The bound request, or the HTTP status and reason the caller gets instead.</returns>
        public RestBinding BindQuery(string methodName, NameValueCollection query)
        {
            var arguments = new List<RestArgument>();

            foreach (string? key in query.AllKeys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                arguments.Add(new RestArgument(key, query[key] ?? ""));
            }

            return Bind(methodName, arguments);
        }

        private RestBinding Bind(string methodName, IReadOnlyList<RestArgument> arguments)
        {
            if (!m_methods.TryGetValue(methodName, out var overloads))
                return RestBinding.Fail(HttpStatusCode.NotFound, $"Method '{methodName}' is not exposed by this service");

            var candidates = overloads.Where(candidate => candidate.Parameters.Length == arguments.Count).ToList();

            if (candidates.Count == 0)
                return RestBinding.Fail(HttpStatusCode.BadRequest, $"Method '{methodName}' does not take {arguments.Count} argument(s)");

            // Named arguments pick the overload whose parameter names they match;
            // otherwise the first overload of the right arity takes them by position.
            var byName = candidates.FirstOrDefault(candidate => candidate.AcceptsNames(arguments));
            var method = byName ?? candidates[0];

            var ordered = byName != null ? method.OrderByName(arguments) : arguments;

            var parameters = new byte[arguments.Count][];

            for (int i = 0; i < ordered.Count; i++)
            {
                if (!TryToJson(ordered[i], method.Parameters[i], out byte[] json, out string? error))
                    return RestBinding.Fail(HttpStatusCode.BadRequest, error!);

                parameters[i] = json;
            }

            return RestBinding.Ok(new WitRequest
            {
                InvocationId = Guid.NewGuid(),
                MethodName = method.Method.Name,
                ContractId = method.ContractId,
                MethodId = method.MethodId,
                Parameters = parameters
            });
        }

        #endregion

        #region Tools

        private static bool IsEventAccessor(MethodInfo method)
        {
            return method.IsSpecialName &&
                   (method.Name.StartsWith("add_", StringComparison.Ordinal) || method.Name.StartsWith("remove_", StringComparison.Ordinal));
        }

        private static bool TryToJson(RestArgument argument, ParameterInfo parameter, out byte[] json, out string? error)
        {
            error = null;

            // A JSON value goes through as it is; JSON null is an empty payload,
            // which the processor turns into a null argument.
            if (argument.Json != null)
            {
                json = argument.Json.Value.ValueKind == JsonValueKind.Null
                    ? Array.Empty<byte>()
                    : Encoding.UTF8.GetBytes(argument.Json.Value.GetRawText());
                return true;
            }

            string text = argument.Text ?? "";
            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

            if (IsTextual(type))
            {
                json = JsonSerializer.SerializeToUtf8Bytes(text);
                return true;
            }

            if (TryParseJson(text, out JsonValueKind kind))
            {
                bool acceptable = IsNumeric(type)
                    ? kind == JsonValueKind.Number || kind == JsonValueKind.Null
                    : type == typeof(bool)
                        ? kind == JsonValueKind.True || kind == JsonValueKind.False || kind == JsonValueKind.Null
                        : true;

                if (acceptable)
                {
                    json = kind == JsonValueKind.Null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(text);
                    return true;
                }
            }

            json = Array.Empty<byte>();
            error = $"Argument '{parameter.Name}' is not a valid {type.Name}: '{text}'";
            return false;
        }

        private static bool IsTextual(Type type)
        {
            return type == typeof(string) || type == typeof(char) || type == typeof(Guid) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
                   type == typeof(Uri) || type.IsEnum;
        }

        private static bool IsNumeric(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) ||
                   type == typeof(double) || type == typeof(float) || type == typeof(decimal);
        }

        private static bool TryParseJson(string text, out JsonValueKind kind)
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                kind = document.RootElement.ValueKind;
                return true;
            }
            catch (JsonException)
            {
                kind = JsonValueKind.Undefined;
                return false;
            }
        }

        #endregion

        #region Nested

        private sealed class RestMethod
        {
            public RestMethod(Type contract, long contractId, MethodInfo method, long methodId)
            {
                Contract = contract;
                ContractId = contractId;
                Method = method;
                MethodId = methodId;
                Parameters = method.GetParameters();
            }

            public bool AcceptsNames(IReadOnlyList<RestArgument> arguments)
            {
                if (arguments.Count == 0 || arguments.Any(argument => argument.Name == null))
                    return false;

                // Either every argument names a real parameter, or they use the
                // positional aliases param1..paramN.
                return arguments.All(argument =>
                    Parameters.Any(parameter => string.Equals(parameter.Name, argument.Name, StringComparison.OrdinalIgnoreCase)) ||
                    IsPositionalAlias(argument.Name!));
            }

            public IReadOnlyList<RestArgument> OrderByName(IReadOnlyList<RestArgument> arguments)
            {
                var ordered = new RestArgument[Parameters.Length];

                for (int i = 0; i < Parameters.Length; i++)
                {
                    string alias = $"{POSITIONAL_PREFIX}{i + 1}";

                    ordered[i] = arguments.FirstOrDefault(argument =>
                                     string.Equals(argument.Name, Parameters[i].Name, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(argument.Name, alias, StringComparison.OrdinalIgnoreCase))
                                 ?? new RestArgument(Parameters[i].Name, (string?)null);
                }

                return ordered;
            }

            private static bool IsPositionalAlias(string name)
            {
                return name.StartsWith(POSITIONAL_PREFIX, StringComparison.OrdinalIgnoreCase) &&
                       int.TryParse(name.Substring(POSITIONAL_PREFIX.Length), out _);
            }

            public Type Contract { get; }

            public long ContractId { get; }

            public MethodInfo Method { get; }

            public long MethodId { get; }

            public ParameterInfo[] Parameters { get; }
        }

        private sealed class RestArgument
        {
            public RestArgument(string? name, JsonElement json)
            {
                Name = name;
                Json = json;
            }

            public RestArgument(string? name, string? text)
            {
                Name = name;
                Text = text;
            }

            public string? Name { get; }

            public JsonElement? Json { get; }

            public string? Text { get; }
        }

        #endregion
    }

    /// <summary>
    /// The outcome of binding an HTTP call to a method: a request to process, or
    /// the HTTP status and reason to answer with.
    /// </summary>
    public sealed class RestBinding
    {
        private RestBinding(WitRequest? request, HttpStatusCode status, string? error)
        {
            Request = request;
            Status = status;
            Error = error;
        }

        public static RestBinding Ok(WitRequest request)
        {
            return new RestBinding(request, HttpStatusCode.OK, null);
        }

        public static RestBinding Fail(HttpStatusCode status, string error)
        {
            return new RestBinding(null, status, error);
        }

        public WitRequest? Request { get; }

        public HttpStatusCode Status { get; }

        public string? Error { get; }
    }
}
