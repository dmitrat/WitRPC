using System;
using OutWit.Common.Rest.Interfaces;

namespace OutWit.Common.Rest.Adapters
{
    public abstract class QueryBuilderAdapterBase<TValue> : IQueryBuilderAdapter
    {
        #region Constructors

        public QueryBuilderAdapterBase()
        {
            Format = "";
        }

        #endregion

        #region IQueryBuilderAdapter

        public string? Convert(object? value)
        {
            if (value == null)
                return null;

            if (value is not TValue converted)
                return null;

            return Convert(converted);
        }

        public string? Convert(object? value, string format)
        {
            if (value == null)
                return null;

            if (value is not TValue converted)
                return null;

            return Convert(converted, format);
        }

        #endregion

        public abstract string? Convert(TValue value);

        public abstract string? Convert(TValue value, string format);

        #region Properties

        public string Format { get; set; }

        #endregion
    }
}
