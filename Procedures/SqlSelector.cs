using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Truffle.Model;
using Truffle.Database;
using Truffle.Utils;
using System.Collections;

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
        public string BuildSelect(string table, string columns, 
            int top=-1, 
            bool distinct = false,
            string orderby = null)
        {
            var command = new StringBuilder("select ");
            if (distinct) command.Append("distinct ");
            if (top != -1) command.Append($"top {top} "); 
            command.Append($"{columns} from {table}");

            string parameters = BuildParameters();
            if (parameters != null)
                command.Append($" where {parameters}");
            if (orderby != null)
                command.Append($" order by {orderby}");
            return command.ToString();
        }

        public string BuildDelete(string table, int top=-1, string orderby = null)
        {
            var command = new StringBuilder("delete ");
            if (top != -1) command.Append($"top {top} ");
            command.Append($"from {table}");

            string parameters = BuildParameters();
            if (parameters != null)
                command.Append($" where {parameters}");
            if (orderby != null)
                command.Append($" order by {orderby}");
            return command.ToString();
        }

        /// <summary>
        /// Builds the select parameters with values stored in this object.
        /// </summary>
        /// <returns>The generated string</returns>
        public string BuildParameters()
        {
            if (builder.Length == 1) return null;
            return builder.ToString() + ")";
        }

        /// <summary>
        /// Builds a select string with values stored in this object.
        /// Values are used as select parameters and the generated string is of the following format:
        /// <code>select {columns} from {table} where {values from the object}</code>
        /// The columns and table to be selected are provided by an SqlObject.
        /// </summary>
        /// <param name="o">The SqlObject that provides column and table information</param>
        /// <returns>The generated string</returns>
        public string BuildSelect(SqlObject o, int top=-1, bool distinct = false, string orderby = null)
        {
            return BuildSelect(o.GetTable(), o.BuildColumnSelector(), top, distinct, orderby);
        }

        public string BuildDelete(SqlObject o, int top=-1, string orderby = null)
        {
            return BuildDelete(o.GetTable(), top, orderby);
        }

        /// <summary>
        /// Retrieves all entries from a database that fit the parameters stored as values in this object.
        /// The table accessed is dependent on the SqlObject supplied.
        /// The entries are mapped to the supplied Type and returned as a List.
        /// </summary>
        /// <param name="database">The database to use</param>
        /// <returns>A List of all mapped objects</returns>
        public async Task<List<T>> BuildObjects<T> (DatabaseConnector database, 
            int top=-1, 
            bool distinct = false,
            string orderby = null) where T : SqlObject 
        {
            Type t = typeof(T);
            T o = (T) Activator.CreateInstance(t);
            string query = BuildSelect(o, top, distinct, orderby);

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

        /// <summary>
        /// Retrieves all entries from a database that fit the parameters stored as values in this object.
        /// The table accessed is dependent on the SqlObject supplied.
        /// The entries are mapped to SqlObjects and returned as a List.
        /// </summary>
        /// <param name="o">The SqlObject that the entries should be mapped to</param>
        /// <param name="database">The database to use</param>
        /// <returns>A List of all mapped SqlObjects</returns>
        public async Task<object> BuildObjects (SqlObject o, 
            DatabaseConnector database, 
            int top=-1, 
            bool distinct = false,
            string orderby = null)
        {
            string query = BuildSelect(o, top, distinct, orderby);

            var results = (List<Dictionary<string, object>>) await database.RunCommandAsync(query, complex: true);
            var listType = typeof(List<>).MakeGenericType(new Type[] { o.GetType() });
            IList ans = (IList)Activator.CreateInstance(listType);

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
        public SqlSelector SetBetween(object a, object b, string column, bool not=false)
        {
            object[] array = {a, b};
            return Set(column, array, not);
        }

        /// <summary>
        /// Sets a selector for a string contained within values in a column.
        /// All rows containing a similar substring to the value are returned.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public SqlSelector SetLike(string column, string value, bool not=false)
        {
            if (value == null) return this;
            var addition = $"[{column}] LIKE '%{value}%'";
            if (builder.Length > 1) builder.Append(" and ");
            if (not) builder.Append("not ");
            builder.Append(addition);
            return this;
        }

        /// <summary>
        /// Sets a selector for a key value pair corresponding to a column in a table.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <param name="value">The value of the column</param>
        public SqlSelector Set(string column, object value, bool not=false)
        {
            var addition = $"[{column}]{SqlUtils.ParseSelector(value)}";
            if (builder.Length > 1) builder.Append(") and (");
            if (not) builder.Append("not ");
            builder.Append(addition);
            return this;
        }

        public SqlSelector Or(string column, object value, bool not=false)
        {
            var addition = $"[{column}]{SqlUtils.ParseSelector(value)}";
            this.builder.Insert(0, "(");
            if (builder.Length > 2) this.builder.Append(") or (");
            if (not) builder.Append("not ");
            this.builder.Append(addition + ")");
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

        /// <summary>
        /// Combines the parameters of two SqlSelectors with an OR conditional
        /// </summary>
        /// <param name="selector">The selector to combine with</param>
        public SqlSelector Or(SqlSelector selector, bool not=false)
        {
            if (builder.Length == 1) 
            {
                builder = selector.GetBuilder();
                return this;
            }
            if (selector.GetBuilder().Length == 1)
                return this;
            var n = not?"NOT ":"";
            builder = new StringBuilder($"(({this.BuildParameters()} OR {n}{selector.BuildParameters()})");
            return this;
        }

        /// <summary>
        /// Combines the parameters of two SqlSelectors with an AND conditional
        /// </summary>
        /// <param name="selector">The selector to combine with</param>
        public SqlSelector And(SqlSelector selector, bool not=false)
        {
            if (builder.Length == 1) 
            {
                builder = selector.GetBuilder();
                return this;
            }
            if (selector.GetBuilder().Length == 1)
                return this;
            var n = not?"NOT ":"";
            builder = new StringBuilder($"(({this.BuildParameters()} AND {n}{selector.BuildParameters()})");
            return this;
        }

        private StringBuilder GetBuilder()
        {
            return this.builder;
        }
    }
}