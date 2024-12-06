using System;
using System.Globalization;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Converters
{
    public abstract class ValueConverterBase : IValueConverter
    {
        #region IValueConverter

        public bool TryConvert(object? origValue, Type destType, out object? destValue)
        {
            destValue = null;

            if (origValue == null)
                return CheckNull(destType);

            if (destType.IsInstanceOfType(origValue))
                return TryCast(origValue, out destValue);

            if (destType.IsEnum)
                return TryConvertEnum(origValue, destType, out destValue);

            if (origValue is string origStringValue)
                return TryConvertString(origStringValue, destType, out destValue);

            if (origValue is TimeSpan timeSpan && destType == typeof(string))
                return TryConvertTimeSpan(timeSpan, out destValue);

            if (origValue is DateTime dateTime && destType == typeof(string))
                return TryConvertDateTime(dateTime, out destValue);

            if(TryCast(origValue, destType, out destValue))
                return true;

            if (TryUnpack(origValue, destType, out destValue))
                return true;

            if (TryRestore(origValue, destType, out destValue))
                return true;

            return false;
        }

        #endregion

        #region Abstract

        protected abstract bool TryUnpack(object origValue, Type destType, out object? destValue);

        protected abstract bool TryRestore(object origValue, Type destType, out object? destValue);

        #endregion

        #region Tools

        private static bool CheckNull(Type destType)
        {
            return destType.IsClass || Nullable.GetUnderlyingType(destType) != null;
        }

        private static bool TryCast(object origValue, out object? destValue)
        {
            destValue = origValue;
            return true;
        }

        private static bool TryCast(object origValue, Type destType, out object? destValue)
        {
            try
            {
                destValue = Convert.ChangeType(origValue, destType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                destValue = null;
                return false;
            }
        }

        private static bool TryConvertEnum(object origValue, Type destType, out object? destValue)
        {
            try
            {
                if (origValue is string str)
                    destValue = Enum.Parse(destType, str, ignoreCase: true);
                else
                    destValue = Enum.ToObject(destType, origValue);
                return true;
            }
            catch
            {
                destValue = null;
                return false;
            }
        }

        private static bool TryConvertString(string origValue, Type destType, out object? destValue)
        {
            destValue = null;

            if (destType == typeof(Guid) && Guid.TryParse(origValue, out Guid guidResult))
            {
                destValue = guidResult;
                return true;
            }

            if (destType == typeof(TimeSpan) && TimeSpan.TryParse(origValue, CultureInfo.InvariantCulture, out TimeSpan timeSpanResult))
            {
                destValue = timeSpanResult;
                return true;
            }

            if (destType == typeof(DateTime) && DateTime.TryParse(origValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTimeResult))
            {
                destValue = dateTimeResult;
                return true;
            }

            return false;
        }

        private static bool TryConvertTimeSpan(TimeSpan origValue, out object? destValue)
        {
            destValue = origValue.ToString("c", CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryConvertDateTime(DateTime origValue, out object? destValue)
        {
            destValue = origValue.ToString("o", CultureInfo.InvariantCulture);
            return true;
        }

        #endregion
    }
}
