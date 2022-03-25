using System;

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
    }
}