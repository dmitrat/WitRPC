using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine;

namespace OutWit.Common.CommandLine
{
    public static class SerializationUtils
    {
        public static string SerializeCommandLine<T>(this T me)
        {
            var arguments = new List<string>();
            foreach (var property in typeof(T).GetProperties())
            {
                var optionAttribute = property.GetCustomAttribute<OptionAttribute>();
                if (optionAttribute == null)
                    continue;

                var value = property.GetValue(me);
                if (value == null)
                    continue;

                var key = !string.IsNullOrEmpty(optionAttribute.LongName)
                    ? $"--{optionAttribute.LongName}"
                    : $"-{optionAttribute.ShortName}";

                if (property.PropertyType == typeof(bool) && (bool)value)
                    arguments.Add(key);
                else if(property.PropertyType != typeof(bool))
                    arguments.Add($"{key} {value}");
            }

            return string.Join(" ", arguments.Where(arg => arg != null));
        }

        public static T DeserializeCommandLine<T>(this string me)
        {
            return me.Split(' ').DeserializeCommandLine<T>();
        }

        public static T DeserializeCommandLine<T>(this string[] me)
        {
            try
            {
                return Parser.Default.ParseArguments<T>(me).Value;
            }
            catch (Exception e)
            {
                return default;
            }
        }
    }
}
