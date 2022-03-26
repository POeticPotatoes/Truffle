using System;
using Newtonsoft.Json.Linq;

namespace Success.Utils
{
    public static class SqlUtils
    {
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
            return raw.ToString();
        }

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
            return $" is between {Parse(data[0])} and {Parse(data[1])}";
        }
    }
}