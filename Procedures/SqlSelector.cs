using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Success.Utils;
using Truffle.Database;
using Truffle.Model;

namespace Truffle.Procedures
{
    public class SqlSelector : SqlEditor
    {
        public List<SqlObject> BuildSelect(SqlObject o, DatabaseConnector database)
        {
            string columns;
            if (typeof(PartialSqlObject).IsInstanceOfType(o))
            {
                columns = "*";
            } else 
            {
                var keys = o.GetAllValues().Keys;
                columns = String.Join(',', keys);
            }

            string query = BuildSelect(o.GetTable(), columns);

            var results = (List<Dictionary<string, object>>) database.RunCommand(query);

            Type[] types = {typeof(Dictionary<string, object>)};
            ConstructorInfo constructor = o.GetType().GetConstructor(BindingFlags.Public, types);
            List<SqlObject> ans = new List<SqlObject>();

            foreach (Dictionary<string, object> dict in results)
            {
                object[] parameters = {dict};
                SqlObject instance = (SqlObject) constructor.Invoke(parameters);
                ans.Add(instance);
            }
            
            return ans;
        }

        public string BuildSelect(string table, string columns)
        {
            Dictionary<string, string> values = GetFields();
            StringBuilder builder = new StringBuilder($"select {columns} from {table} where ");

            foreach (string key in values.Keys)
            {
                builder.Append(key);
                builder.Append(values[key]);
                builder.Append(" and ");
            }
            builder.Length -= 5;
            return builder.ToString();
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