using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Trufle.Procedures;
using Truffle.Model;
using Truffle.Database;
using Truffle.Utils;

namespace Truffle.Procedures
{
    /// <summary>
    /// Provides utility in selecting values from a database.
    /// </summary>
    public class SqlSelector
    {
        private StringBuilder builder = new StringBuilder("(");

        /// <summary>
        /// Builds a select string with values stored in this object.
        /// Values are used as select parameters and the generated string is of the following format:
        /// <code>select {columns} from {table} where {values from the object}</code>
        /// </summary>
        /// <param name="table">The table to select from</param>
        /// <param name="columns">The columns to select</param>
        /// <returns>The generated string</returns>
        public string BuildSelect(string table, string columns)
        {
            string command = $"select {columns} from {table}";

            string parameters = BuildParameters();
            if (parameters == null) return command;

            return $"{command} where {parameters}";
        }

        /// <summary>
        /// Builds the select parameters with values stored in this object.
        /// </summary>
        /// <returns>The generated string</returns>
        public string BuildParameters()
        {
            if (builder.Length == 1) return null;
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Builds a select string with values stored in this object.
        /// Values are used as select parameters and the generated string is of the following format:
        /// <code>select {columns} from {table} where {values from the object}</code>
        /// The columns and table to be selected are provided by an SqlObject.
        /// </summary>
        /// <param name="o">The SqlObject that provides column and table information</param>
        /// <returns>The generated string</returns>
        public string BuildSelect(SqlObject o)
        {
            return BuildSelect(o.GetTable(), o.BuildColumnSelector());
        }

        /// <summary>
        /// Retrieves all entries from a database that fit the parameters stored as values in this object.
        /// The entries are mapped to SqlObjects and returned as a List.
        /// </summary>
        /// <param name="o">The SqlObject that the entries should be mapped to</param>
        /// <param name="database">The database to use</param>
        /// <returns>A List of all mapped SqlObjects</returns>
        public async Task<List<T>> BuildObjects<T> (DatabaseConnector database) where T : SqlObject 
        {
            Type t = typeof(T);
            T o = (T) Activator.CreateInstance(t);
            string query = BuildSelect(o);

            var results = (List<Dictionary<string, object>>) await database.RunCommandAsync(query, complex: true);
            var ans = new List<T>();

            foreach (Dictionary<string, object> dict in results)
            {
                T instance = (T) Activator.CreateInstance(t);
                instance.LoadValues(dict);
                ans.Add((T) instance);
            }
            
            return ans;
        }

        public async Task<List<SqlObject>> BuildObjects (SqlObject o, DatabaseConnector database)
        {
            string query = BuildSelect(o);

            var results = (List<Dictionary<string, object>>) await database.RunCommandAsync(query, complex: true);
            var ans = new List<SqlObject>();

            foreach (Dictionary<string, object> dict in results)
            {
                var instance = (SqlObject) Activator.CreateInstance(o.GetType());
                instance.LoadValues(dict);
                ans.Add(instance);
            }
            
            return ans;
        }

        /// <summary>
        /// Sets a selector for a range of values (eg. between certain dates) corresponding to a column in a table.
        /// </summary>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        /// <param name="column">The name of the column</param>
        public SqlSelector SetBetween(object a, object b, string column)
        {
            object[] array = {a, b};
            return Set(column, array);
        }

        public SqlSelector SetLike(string column, object value)
        {
            if (value == null) return this;
            var addition = $"{column} LIKE '%{value}%'";
            if (builder.Length > 1) builder.Append(" and ");
            builder.Append(addition);
            return this;
        }

        public SqlSelector Set(string column, object value)
        {
            var addition = $"{column}{SqlUtils.ParseSelector(value)}";
            if (builder.Length > 1) builder.Append(" and ");
            builder.Append(addition);
            return this;
        }

        /// <summary>
        /// Saves all keys and values from a Dictionary.
        /// </summary>
        /// <param name="toAdd">The Dictionary to add</param>
        public SqlSelector SetAll(Dictionary<string, object> toAdd)
        {
            foreach (string column in toAdd.Keys)
            {
                Set(column, toAdd[column]);
            }
            return this;
        }

        public SqlSelector Or(SqlSelector selector)
        {
            if (builder.Length == 1) 
            {
                builder = selector.GetBuilder();
                return this;
            }
            if (selector.GetBuilder().Length == 1)
                return this;
            builder = new StringBuilder($"({builder.ToString()}) OR {selector.BuildParameters()}");
            return this;
        }

        public SqlSelector And(SqlSelector selector)
        {
            if (builder.Length == 1) 
            {
                builder = selector.GetBuilder();
                return this;
            }
            if (selector.GetBuilder().Length == 1)
                return this;
            builder = new StringBuilder($"({builder.ToString()}) AND {selector.BuildParameters()}");
            return this;
        }

        private StringBuilder GetBuilder()
        {
            return this.builder;
        }
    }
}