using System;
using System.Collections.Generic;
using System.Reflection;
using Truffle.Database;

namespace Truffle.Model
{
    /// <summary>
    /// An extension of SqlObject that stores all columns from a table on top of the defined columns, and provides methods
    /// for accessing and reading them. This class should be extended by models where column names might be dynamic or data does not need to be read locally.
    /// </summary>
    public abstract class PartialSqlObject : SqlObject
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();

        /// <summary>
        /// Initialises an empty instance of a PartialSqlObject
        /// </summary>
        public PartialSqlObject(): base() {}

        /// <summary>
        /// Initialises a new instance of a PartialSqlObject with a Dictionary containing column values
        /// </summary>
        /// <param name="values">The values to be stored and accessed in the PartialSqlObject</param>
        public PartialSqlObject(Dictionary<string, object> values): base(values) {}

        /// <summary>
        /// <para> Initialises a new instance of a PartialSqlObject with a value in the key column in a database.
        /// (A property marked with the Id annotation in this object). Retrieves all column values for the first entry found and initialises itself with them</para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {Id}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the key column</param>
        /// <param name="database">The database to connect to</param>
        public PartialSqlObject(object value, DatabaseConnector database): base(value, database) {}

        /// <summary>
        /// <para> Initialises a new instance of a PartialSqlObject with a column and value pair in a database.
        /// Retrieves all column values for the first entry found and initialises itself with them. </para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {column}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the column</param>
        /// <param name="column">The name of the column</param>
        /// <param name="database">The database to connect to</param>
        public PartialSqlObject(object value, string column, DatabaseConnector database): base(value, column, database) {}

        public override void LoadValues(Dictionary<string, object> values)
        {
            base.LoadValues(values);
            data = values;
        }

        public override Dictionary<string, object> GetAllValues(bool ignoreIdentities=false)
        {
            var ignore = new List<string>();
            foreach (KeyValuePair<string, PropertyInfo> c in GetColumns())
            {
                if (ignoreIdentities && c.Value.GetCustomAttribute(typeof(IdentityAttribute)) != null)
                    ignore.Add(c.Value.Name);

                data[c.Key] =  c.Value.GetValue(this);
            }

            var ans = new Dictionary<string, object>();
            foreach (var e in data)
            {
                if (ignoreIdentities && ignore.Contains(e.Key)) continue;
                ans[e.Key] = e.Value;
            }

            return ans;
        }

        /// <summary>
        /// Retrieves a value of a column from the object.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The value of the column, or null if it isn't present.</returns>
        public override object GetValue(string column) 
        {
            if (this.HasColumn(column))
                data[column] = base.GetValue(column);
            if (!data.ContainsKey(column)) return null;
            var o = data[column];
            if (typeof(System.DBNull).IsInstanceOfType(o)) return null;
            return o;
        }

        public override void SetValue(string column, object o) 
        {
            if (HasColumn(column))
                SetValue(column, o);
            data[column] = o;
        }

        public override void LogValues()
        {
            Dictionary<string, object> values = GetAllValues();
            foreach (string key in values.Keys)
            {
                Console.WriteLine($"{key}: {values[key]} of type {values[key].GetType()}");
            }
        }

        /// <summary>
        /// Returns the column list for a query to a database for this object. * is returned for a PartialSqlObject.
        /// </summary>
        /// <returns></returns>
        public override string BuildColumnSelector()
        {
            return "*";
        }
    }
}
