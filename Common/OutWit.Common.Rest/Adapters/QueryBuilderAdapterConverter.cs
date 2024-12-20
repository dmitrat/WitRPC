using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Adapters
{
    public class QueryBuilderAdapterConverter<TValue> : QueryBuilderAdapterBase<TValue>
    {
        public QueryBuilderAdapterConverter(string format, QueryBuilderConverter<TValue> converter)
        {
            Format = format;
            Converter = converter;
        }

        public override string? Convert(TValue value)
        {
            return Converter(value, Format);
        }

        public override string? Convert(TValue value, string format)
        {
            return Converter(value, format);
        }

        private QueryBuilderConverter<TValue> Converter { get; }
    }

    public delegate string QueryBuilderConverter<TValue>(TValue value, string format);
}
