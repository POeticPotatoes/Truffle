using System;
using Newtonsoft.Json.Linq;

namespace Truffle.Utils
{
    /// <summary>
    /// Provides static methods for parsing objects into Sql compatible formats
    /// </summary>
    public static class SqlUtils
    {
        /// <summary>
        /// Parses a generic object into an Sql compatible format
        /// </summary>
        public static string Parse(object raw)
        {
            if (raw == null) return "null";
            switch (raw)
            {
                case string:
                    return $"'{raw}'";
                case int:
                    return $"{raw}";
                case float:
                    return $"{raw}";
                case DateTime:
                    return $"'{((DateTime)raw).ToString("yyyy-MM-dd")}'";
                case bool:
                    return ((bool)raw)?"1":"0";
                case double:
                    return $"{raw}";
            }
            return $"'{raw.ToString()}'";
        }

        /// <summary>
        /// Parses a generic object into a string to be used for a select query
        /// </summary>
        public static string ParseSelector(object raw)
        {
            if (raw == null) return " IS NULL";

            object[] data;
            switch (raw)
            {
            case JArray:
                data = ((JArray) raw).ToObject<object[]>();
                break;
            case object[]:
                data = (object[]) raw;
                break;
            default:
                return $"={Parse(raw)}";
            }
            return $" between {Parse(data[0])} and {Parse(data[1])}";
        }

        /// <summary>
        /// Parses a DateTime into an Sql date format
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static DateTime ParseDate(object raw)
        {
            return DateTime.ParseExact(((string) raw).Substring(0, 10),"yyyy-MM-dd",null);
        }
    }
}