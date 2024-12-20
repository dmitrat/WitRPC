using System;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace OutWit.Common.Rest.Adapters
{
    public class QueryBuilderAdapterEnum: QueryBuilderAdapterBase<Enum>
    {
        #region Constants

        private const char DOUBLE_QUOTES = '"';

        #endregion

        #region Constructors

        public QueryBuilderAdapterEnum()
        {
            Format = "";
        }

        #endregion

        #region Functions

        public override string? Convert(Enum value)
        {
            return JsonConvert.SerializeObject(value, new StringEnumConverter()).Trim(DOUBLE_QUOTES);
        }

        public override string? Convert(Enum value, string format)
        {
            return JsonConvert.SerializeObject(value, new StringEnumConverter()).Trim(DOUBLE_QUOTES);
        }

        #endregion
    }
}
