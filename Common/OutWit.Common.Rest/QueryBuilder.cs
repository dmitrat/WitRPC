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

        public QueryBuilder AddParameter<TValue>(string name, IEnumerable<TValue> values)
        {
            var parameters = new List<string>();
            foreach (var value in values)
            {
                var parameter = value.Convert();
                if (string.IsNullOrEmpty(parameter))
                    continue;

                parameters.Add(parameter);
            }

            if (parameters.Count == 0)
                return this;

            AddParameter(name, parameters.ToArray());

            return this;
        }

        public QueryBuilder AddParameter<TValue>(string name, IEnumerable<TValue> values, string format)
        {
            var parameters = new List<string>();
            foreach (var value in values)
            {
                var parameter = value.Convert(format);
                if (string.IsNullOrEmpty(parameter))
                    continue;

                parameters.Add(parameter);
            }

            if (parameters.Count == 0)
                return this;

            AddParameter(name, parameters.ToArray());

            return this;
        }

        public QueryBuilder AddParameter<TValue>(string name, TValue? value)
        {
            if (value == null)
                return this;

            var parameter = value.Convert();
            if (string.IsNullOrEmpty(parameter))
                return this;

            AddParameter(name, parameter);

            return this;
        }

        public QueryBuilder AddParameter<TValue>(string name, TValue? value, string format)
        {
            if (value == null)
                return this;

            var parameter = value.Convert(format);
            if (string.IsNullOrEmpty(parameter))
                return this;

            AddParameter(name, parameter);

            return this;
        }

        public QueryBuilder AddParameter(string name, object? value)
        {
            if (value == null)
                return this;

            var parameter = value.Convert();
            if (string.IsNullOrEmpty(parameter))
                return this;

            AddParameter(name, parameter);

            return this;
        }

        public QueryBuilder AddParameter(string name, object? value, string format)
        {
            if (value == null)
                return this;

            var parameter = value.Convert(format);
            if (string.IsNullOrEmpty(parameter))
                return this;

            AddParameter(name, parameter);

            return this;
        }

        public QueryBuilder AddParameter(string name, params string[] values)
        {
            return values.Length != 0
                ? AddParameter(name, string.Join(LIST_SEPARATOR, values))
                : this;
        }

        #endregion

        #region Properties

        private IDictionary<string, string> Parameters { get; }

        #endregion

    }
}