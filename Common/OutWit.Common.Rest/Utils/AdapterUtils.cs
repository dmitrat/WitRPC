using System;
using System.Collections.Generic;
using System.Globalization;
using OutWit.Common.Collections;
using OutWit.Common.Rest.Adapters;
using OutWit.Common.Rest.Interfaces;

namespace OutWit.Common.Rest.Utils
{
    public static class AdapterUtils
    {
        #region Constructors

        static AdapterUtils()
        {
            Adapters = new Dictionary<Type, IQueryBuilderAdapter>();

            Register<byte>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<sbyte>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));

            Register<short>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<ushort>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));

            Register<int>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<uint>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));

            Register<long>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<ulong>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));

            Register<char>("", (val, _) => val.ToString(CultureInfo.InvariantCulture));
            Register<string>("", (val, _) => val.ToString(CultureInfo.InvariantCulture));

            Register<bool>("", (val, _) => val.ToString(CultureInfo.InvariantCulture).ToLower());

            Register<float>("", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<double>("", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));


            Register<Guid>("D", (val, format) => val.ToString(format, CultureInfo.InvariantCulture).ToUpper());

            Register<TimeSpan>("c", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<DateOnly>("d", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<TimeOnly>("T", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<DateTimeOffset>("G", (val, format) => val.ToString(format, CultureInfo.InvariantCulture));
            Register<DateTime>("G", (val, format) =>
            {
                if (val.Kind == DateTimeKind.Unspecified)
                    val = DateTime.SpecifyKind(val, DateTimeKind.Utc);

                return val.ToString(format, CultureInfo.InvariantCulture);
            });

            Register(new QueryBuilderAdapterEnum());
        }

        #endregion

        #region Initialization

        public static void Register<TValue>(QueryBuilderAdapterBase<TValue> adapter)
        {
            Adapters.AddOrUpdate(typeof(TValue), adapter);
        }

        public static void Register<TValue>(string format, QueryBuilderConverter<TValue> converter)
        {
            Register(new QueryBuilderAdapterConverter<TValue>(format, converter));
        }

        #endregion

        #region Functions

        public static bool CanAppend(this Type type)
        {
            if (type.BaseType == typeof(Enum))
                return Adapters.ContainsKey(typeof(Enum));

            return Adapters.ContainsKey(type);
        }

        public static bool CanAppend(this object val)
        {
            if (val is Enum enumVal)
                return Adapters.ContainsKey(typeof(Enum));

            return Adapters.ContainsKey(val.GetType());
        }

        public static string? ConvertEnum<TEnum>(this TEnum val)
            where TEnum : Enum
        {
            if (!Adapters.TryGetValue(typeof(Enum), out IQueryBuilderAdapter? adapter))
                return null;

            return adapter.Convert(val);
        }

        public static string? Convert(this object? val)
        {
            if (val == null)
                return null;

            if (val is Enum enumVal)
                return ConvertEnum(enumVal);

            if (!Adapters.TryGetValue(val.GetType(), out IQueryBuilderAdapter? adapter))
                return null;

            return adapter.Convert(val);
        }

        public static string? Convert(this object? val, string format)
        {
            if (val == null)
                return null;

            if (val is Enum enumVal)
                return ConvertEnum(enumVal);

            if (!Adapters.TryGetValue(val.GetType(), out IQueryBuilderAdapter? adapter))
                return null;

            return adapter.Convert(val, format);
        }

        public static string? GetFormatFor<T>()
        {
            if (!Adapters.TryGetValue(typeof(T), out IQueryBuilderAdapter? adapter))
                return null;

            return adapter.Format;
        }

        public static bool SetFormatFor<T>(string format)
        {
            if (!Adapters.TryGetValue(typeof(T), out IQueryBuilderAdapter? adapter))
                return false;

            adapter.Format = format;
            return true;
        }

        #endregion

        #region Properties

        private static Dictionary<Type, IQueryBuilderAdapter> Adapters { get; }

        #endregion
    }
}
