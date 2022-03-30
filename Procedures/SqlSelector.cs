using System;
using System.Collections.Generic;
using System.Text;
using Success.Utils;
using Truffle.Database;
using Truffle.Model;

namespace Truffle.Procedures
{
    public class SqlSelector : SqlEditor
    {
        public string BuildSelect(string table, string columns)
        {
            Dictionary<string, string> values = GetFields();
            StringBuilder builder = new StringBuilder($"select {columns} from {table}");

            if (values.Count == 0) return builder.ToString();

            builder.Append(" where ");

            foreach (string key in values.Keys)
            {
                builder.Append(key);
                builder.Append(values[key]);
                builder.Append(" and ");
            }
            builder.Length -= 5;
            return builder.ToString();
        }

        public List<SqlObject> BuildObjects(SqlObject o, DatabaseConnector database)
        {
            string columns = o.BuildColumnSelector();
            string query = BuildSelect(o.GetTable(), columns);

            var results = (List<Dictionary<string, object>>) database.RunCommand(query, complex: true);
            var ans = new List<SqlObject>();

            foreach (Dictionary<string, object> dict in results)
            {
                SqlObject instance = (SqlObject) Activator.CreateInstance(o.GetType());
                instance.LoadValues(dict);
                ans.Add(instance);
            }
            
            return ans;
        }

        public void SetBetween(object a, object b, string column)
        {
            object[] array = {a, b};
            Set(column, array);
        }

        protected override string Parse(object o)
        {
            return SqlUtils.ParseSelector(o);
        }
    }
}