using System;
using System.Collections.Generic;
using System.Reflection;
using Truffle.Database;
using Truffle.Utils;

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
        public PartialSqlObject(Dictionary<string, object> values): base(values)
        {
            data = values;
        }

        /// <summary>
        /// <para> Initialises a new instance of a PartialSqlObject with a value in the key column in a database.
        /// (A property marked with the Id annotation in this object). Retrieves all column values for the first entry found and initialises itself with them</para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {Id}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the key column</param>
        /// <param name="database">The database to connect to</param>
        public PartialSqlObject(object value, DatabaseConnector database)
        {
            string req = BuildRequest(value, GetId());

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{value} was not present in the database");

            LoadValues(response[0]);
        }

        /// <summary>
        /// <para> Initialises a new instance of a PartialSqlObject with a column and value pair in a database.
        /// Retrieves all column values for the first entry found and initialises itself with them. </para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {column}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the column</param>
        /// <param name="column">The name of the column</param>
        /// <param name="database">The database to connect to</param>
        public PartialSqlObject(object value, string column, DatabaseConnector database)
        {  
            string req = BuildRequest(value, column);

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{column} of value {SqlUtils.Parse(value)} was not present in the database");

            LoadValues(response[0]);
        }

        public override void LoadValues(Dictionary<string, object> values)
        {
            base.LoadValues(values);
            data = values;
        }

        /// <summary>
        /// Retrieves a value of a column from the object.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The value of the column, or null if it isn't present.</returns>
        public object GetValue(string column) 
        {
            var p = this.GetType().GetProperty(column);
            if (p != null) data[column] = p.GetValue(this);
            if (!data.ContainsKey(column)) return null;
            var o = data[column];
            if (typeof(System.DBNull).IsInstanceOfType(o)) return null;
            return o;
        }

        public void SetValue(string column, object o) 
        {
            if (!data.ContainsKey(column)) return;
            data[column] = o;
        }

        public override Dictionary<string, object> GetAllValues()
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                var v = p.GetValue(this);
                if (v == null && data.ContainsKey(attribute.Name)) continue;
                data[attribute.Name] =  p.GetValue(this);
            }

            return data;
        }

        /// <summary>
        /// Returns the value of a column cast as a bool.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The boolean value of the column, or false if it isn't present.</returns>
        public bool GetBoolean(string column) 
        {
            object o = GetValue(column);
            return o==null?false:(bool) o;
        }

        /// <summary>
        /// Returns the value of a column cast as a string.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The string value of the column, or null if it isn't present.</returns>
        public string GetString(string column) 
        {
            return (string) GetValue(column);
        }

        /// <summary>
        /// Returns the value of a column cast as a DateTime.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The date value of the parameter, or null if it isn't present.</returns>
        public DateTime? GetDate(string column) 
        {
            return (DateTime?) GetValue(column);
        }

        /// <summary>
        /// Returns the value of a column cast as an int.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The int value of the column, or 0 if it isn't present.</returns>
        public int GetInt(string column) 
        {
            object o = GetValue(column);
            return o==null?0:(int) o;
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