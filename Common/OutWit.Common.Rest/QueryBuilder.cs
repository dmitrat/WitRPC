using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OutWit.Common.Rest.Utils;

namespace OutWit.Common.Rest
{
    public class QueryBuilder
    {
        #region Constants

        private const string LIST_SEPARATOR = ",";

        #endregion

        #region Constructors

        public QueryBuilder()
        {
            Parameters = new Dictionary<string, string>();
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return AsStringAsync().GetAwaiter().GetResult();
        }

        public async Task<string> AsStringAsync()
        {
            using var content = new FormUrlEncodedContent(Parameters);

            return await content.ReadAsStringAsync();
        }

        public QueryBuilder AddParameter(string name, string? value)
        {
            if (value is not null)
                Parameters.Add(name, value);
            
            return this;
        }

        public QueryBuilder AddParameter(string name, bool? value)
        {
            return AddParameter(name, value, Extensions.ToBoolString);
        }

        public QueryBuilder AddParameter<TValue>(string name, TValue? value)
            where TValue : struct, Enum
        {
            return AddParameter(name, value, Extensions.ToEnumString);
        }

        public QueryBuilder AddParameter<TValue>(string name, TValue value)
            where TValue : struct, Enum
        {
            return AddParameter(name, value.ToEnumString());
        }

        public QueryBuilder AddParameter<TValue>(string name, IEnumerable<TValue>? values)
            where TValue : struct, Enum
        {
            return AddParameter(name, values, Extensions.ToEnumString);
        }

        public QueryBuilder AddParameter(string name, DateTime? value, string format)
        {
            return AddParameter(name, value, time => time.ToDateTimeString(format));
        }

        public QueryBuilder AddParameter(string name, params string[] values)
        {
            return values.Length != 0
                ? AddParameter(name, string.Join(LIST_SEPARATOR, values))
                : this;
        }

        public QueryBuilder AddParameter(string name, long? value)
        {
            return AddParameter(name, value, Extensions.ToIntegerString);
        }
            

        public QueryBuilder AddParameter(string name, double? value)
        {
            return AddParameter(name, value, Extensions.ToDoubleString);
        }

        private QueryBuilder AddParameter<TValue>(string name, TValue? value, Func<TValue, string> converter)
            where TValue : struct
        {
            return value.HasValue
                ? AddParameter(name, converter(value.Value))
                : this;
        }

        private QueryBuilder AddParameter<TValue>(string name, IEnumerable<TValue>? values, Func<TValue, string> converter)
            where TValue : struct
        {
            return values is not null
                ? AddParameter(name, string.Join(LIST_SEPARATOR, values.Select(converter)))
                : this;
        }

        #endregion


        #region Properties

        private IDictionary<string, string> Parameters { get; }

        #endregion

    }
}