using System;
using System.Collections.Generic;
using System.Reflection;
using Success.Utils;
using Truffle.Database;
using Truffle.Procedures;

namespace Truffle.Model
{
    /// <summary>
    /// <para> Represents an entry to a database and provides methods to retrieve and update itself.
    /// When extended, this class maps values retrieved to properties in its children that are marked 
    /// with the corresponding Column and Id tags.</para>
    /// </summary>
    public abstract class SqlObject
    {
        /// <summary>
        /// Initialises an empty instance of an SqlObject
        /// </summary>
        public SqlObject() {}

        /// <summary>
        /// <para> Initialises a new instance of an SqlObject with a value in the key column in a database.
        /// (A property marked with the Id annotation in this object). Retrieves declared column values for the first entry found and initialises itself with them</para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select {columns} from {table} where {Id}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the key column</param>
        /// <param name="database">The database to connect to</param>
        public SqlObject(object value, DatabaseConnector database)
        {
            string req = BuildRequest(value, GetId());

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{value} was not present in the database");

            LoadValues(response[0]);
        }

        /// <summary>
        /// <para> Initialises a new instance of an SqlObject with a column and value pair in a database.
        /// Retrieves declared column values for the first entry found and initialises itself with them. </para>
        /// 
        /// <para> The query is generated with BuildRequest() and would be similar to this: <code> select {columns} from {table} where {column}={value}</code> </para>
        /// </summary>
        /// <param name="value">The value of the column</param>
        /// <param name="column">The name of the column</param>
        /// <param name="database">The database to connect to</param>
        public SqlObject(object value, string column, DatabaseConnector database)
        {
            string req = BuildRequest(value, column);

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{column} of value {SqlUtils.Parse(value)} was not present in the database");

            LoadValues(response[0]);
        }

        /// <summary>
        /// Initialises a new instance of an SqlObject with a Dictionary containing column values
        /// </summary>
        /// <param name="values">The values to be stored and accessed in the object</param>
        public SqlObject(Dictionary<string, object> values)
        {
            LoadValues(values);
        }

        /// <summary>
        /// Builds a string that can be used to select all relevant columns and return all entries from a table.
        /// </summary>
        /// <returns>The generated string.</returns>
        public virtual string BuildAllRequest() 
        {
            
            return $"SELECT {BuildColumnSelector()} FROM {GetTable()}";
        }

        /// <summary>
        /// Returns the name of the Id column for the object. If no column has been marked with the id annotation, this returns null.
        /// </summary>
        /// <returns>The name of the Id column</returns>
        public string GetId()
        {
            return GetId(this.GetType());
        }

        /// <summary>
        /// Returns the value of the table annotation for this object.
        /// </summary>
        /// <returns>The value of the table annotation</returns>
        public string GetTable()
        {
            return GetTable(this.GetType());
        }

        /// <summary>
        /// Retrieves all values stored in the object as a Dictionary.
        /// </summary>
        /// <returns>All values in the object.</returns>
        public virtual Dictionary<string, object> GetAllValues()
        {
            Dictionary<string, object> ans = new Dictionary<string, object>();

            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                ans.Add(attribute.Name, p.GetValue(this));
            }
            return ans;
        }

        /// <summary>
        /// Logs all all keys, values and types stored in the object.
        /// </summary>
        public virtual void LogValues()
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                Console.WriteLine($"{p.Name}: {p.GetValue(this)} of type {p.GetType()}");
            }
        }

        /// <summary>
        /// Creates a new entry in a database with values stored in this object.
        /// </summary>
        /// <param name="database">The database to create a new entry in</param>
        /// <returns>Whether the entry creation was successful</returns>
        public virtual bool Create(DatabaseConnector database) 
        {
            SqlInserter inserter = new SqlInserter(this);
            return inserter.Insert(GetTable(),database);
        }

        /// <summary>
        /// Updates an existing entry in a database with values stored in this object.
        /// </summary>
        /// <param name="database">The database to update</param>
        /// <returns>Whether the entry updating was successful</returns>
        public virtual bool Update(DatabaseConnector database) 
        {
            SqlUpdater updater = new SqlUpdater(this);
            return updater.Update(GetTable(),database);
        }

        /// <summary>
        /// Loads a Dictionary of values into the object. Keys with corresponding column values are mapped and stored.
        /// </summary>
        /// <param name="values"></param>
        public void LoadValues(Dictionary<string, object> values)
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                try {
                    var value = values[attribute.Name];
                    if (typeof(System.DBNull).IsInstanceOfType(value))
                    {
                        p.SetValue(this, null);
                        continue;
                    }
                    if (typeof(DateTime).Equals(p.GetType()) && typeof(string).IsInstanceOfType(value))
                    {
                        p.SetValue(this, SqlUtils.ParseDate(value));
                        continue;
                    }
                    p.SetValue(this, value);
                } catch (Exception e)
                {Console.WriteLine(e.Message); Console.WriteLine(e.StackTrace);}
            }
        }

        /// <summary>
        /// Returns the name of the Id column for a given Type. If no column has been marked with the id annotation, this returns null.
        /// </summary>
        /// <returns>The name of the Id column</returns>
        protected static string GetId(Type t)
        {
            foreach (PropertyInfo p in t.GetProperties())
            {
                var attribute = (IdAttribute) p.GetCustomAttribute(typeof(IdAttribute));
                if (attribute == null) continue;

                var column = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                return column.Name;
            }
            
            return null;
        }

        /// <summary>
        /// Returns the value of the table annotation for a given Type.
        /// </summary>
        /// <returns>The value of the table annotation</returns>
        protected static string GetTable(Type t)
        {
            var table = (TableAttribute) t.GetCustomAttribute(typeof(TableAttribute));
            return table.Name;
        }

        /// <summary>
        /// Builds a select string for an entry with a provided key value pair. 
        /// If multiple parameters are required, an SqlSelector should be used instead.
        /// </summary>
        /// <param name="value">The value of the column</param>
        /// <param name="column">The name of the column</param>
        /// <returns>The generated select string</returns>
        protected string BuildRequest(object value, string column) 
        {
            string val = SqlUtils.Parse(value);
            return $"SELECT {BuildColumnSelector()} FROM {GetTable()} WHERE {column}={val}";
        }

        /// <summary>
        /// Returns the column list for a query to a database for this object.
        /// </summary>
        /// <returns></returns>
        public virtual string BuildColumnSelector()
        {
            List<string> values = new List<string>();
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                values.Add(attribute.Name);
            }

            return String.Join(",", values);
        }
    }
}