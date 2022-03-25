using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Truffle.Model;

namespace Truffle.Procedures
{
    public class SqlSelector : SqlEditor
    {
        public string BuildSelect(string table)
        {
            Dictionary<string, string> values = GetFields();
            StringBuilder builder = new StringBuilder($"select * from {table} where ");

            foreach (string key in values.Keys)
            {
                builder.Append(key);
                builder.Append(values[key]);
                builder.Append(" and ");
            }
            builder.Length -= 5;
            return builder.ToString();
        }

        public string BuildSelect(SqlObject o)
        {
            var columns = o.GetAllValues().Keys;
            Dictionary<string, string> values = GetFields();
            StringBuilder builder = new StringBuilder($"select {String.Join(',', columns)} from {o.GetTable()} where ");

            foreach (string key in values.Keys)
            {
                builder.Append(key);
                builder.Append(Parse(values[key]));
                builder.Append(" and ");
            }
            return "";
        }

        public void SetBetween(object a, object b, string column)
        {
            object[] array = {a, b};
            Set(column, array);
        }

        protected override string Parse(object o)
        {
            if (o == null) return " IS NULL";

            object[] data;
            switch (o)
            {
            case JArray:
                data = ((JArray) o).ToObject<object[]>();
                break;
            case object[]:
                data = (object[]) o;
                break;
            default:
                return $"={base.Parse(o)}";
            }
            return $" is between {base.Parse(data[0])} and {base.Parse(data[1])}";
        }
    }
}