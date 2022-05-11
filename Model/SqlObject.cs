using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.WindowsAzure.Storage.Table;
using Truffle.Database;
using Truffle.Procedures;
using Truffle.Utils;
using Truffle.Validation;
using Trufle.Procedures;

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
            initFromDatabase(value, GetId(), database);
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
            initFromDatabase(value, column, database);
        }

        protected void initFromDatabase(object value, string column, DatabaseConnector database)
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
        public string BuildAllRequest(int top = -1, string orderBy = null) 
        {
            string num = top==-1?"":$"TOP {top} ",
                   order = orderBy == null?"": $" order by {orderBy}";
            return $"SELECT {num}{BuildColumnSelector()} FROM {GetTable()}{order}";
        }

        /// <summary>
        /// Returns the name of the Id column for the object. If no column has been marked with the id annotation, this returns null.
        /// </summary>
        /// <returns>The name of the Id column</returns>
        public virtual string GetId()
        {
            foreach (var p in GetColumns<IdAttribute> ())
            {
                var column = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                return column.Name;
            }
            
            return null;
        }

        public virtual object GetIdValue()
        {
            foreach (var p in GetColumns<IdAttribute> ())
            {
                return p.GetValue(this);
            }
            
            return null;
        }

        /// <summary>
        /// Returns the value of the table annotation for this object.
        /// </summary>
        /// <returns>The value of the table annotation</returns>
        public virtual string GetTable()
        {
            var table = (TableAttribute) this.GetType().GetCustomAttribute(typeof(TableAttribute));
            return table.Name;
        }

        /// <summary>
        /// Retrieves all values stored in the object as a Dictionary.
        /// </summary>
        /// <returns>All values in the object.</returns>
        public virtual Dictionary<string, object> GetAllValues(bool ignoreIdentities=false)
        {
            Dictionary<string, object> ans = new Dictionary<string, object>();

            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;
                if (ignoreIdentities && p.GetCustomAttribute(typeof(IdentityAttribute)) != null)
                    continue;

                ans.Add(attribute.Name, p.GetValue(this));
            }
            return ans;
        }

        /// <summary>
        /// Logs all all keys, values and types stored in the object.
        /// </summary>
        public virtual void LogValues()
        {
            foreach (var p in GetColumns<ColumnAttribute> ())
            {
                Console.WriteLine($"{p.Name}: {p.GetValue(this)} of type {p.GetType()}");
            }
        }

        /// <summary>
        /// Creates a new entry in a database with values stored in this object.
        /// </summary>
        /// <param name="database">The database to create a new entry in</param>
        public virtual void Create(DatabaseConnector database, bool validate=true) 
        {
            var inserter = CreateEditor<SqlInserter>();
            inserter.Insert(GetTable(),database);
        }

        /// <summary>
        /// Creates a new entry in a database asynchronously with values stored in this object.
        /// </summary>
        /// <param name="database">The database to create a new entry in</param>
        public virtual async Task CreateAsync(DatabaseConnector database, bool validate=true) 
        {
            var inserter = CreateEditor<SqlInserter>();
            await inserter.InsertAsync(GetTable(),database);
        }

        /// <summary>
        /// Updates an existing entry in a database asynchronously with values stored in this object.
        /// </summary>
        /// <param name="database">The database to update</param>
        public void Update(DatabaseConnector database, bool validate=true) 
        {
            var updater = CreateEditor<SqlUpdater>();
            updater.Update(GetTable(),database);
        }

        /// <summary>
        /// Updates an existing entry in a database with values stored in this object.
        /// </summary>
        /// <param name="database">The database to update</param>
        public async Task UpdateAsync(DatabaseConnector database, bool validate=true) 
        {
            var updater = CreateEditor<SqlUpdater>();
            await updater.UpdateAsync(GetTable(),database);
        }

        public T CreateEditor<T>(bool validate = true) where T: SqlEditor
        {
            if (validate)
            {
                Clean();
                Validate();
            }
            return (T) Activator.CreateInstance(typeof(T), new object[] {this});
        }

        /// <summary>
        /// Checks if this SqlObject is equal to another SqlObject. 
        /// Returns true if all its stored values are the same. 
        /// Ignores class extension.
        /// </summary>
        /// <param name="o">The SqlObject to compare with</param>
        public bool Equals(SqlObject o)
        {
            Dictionary<string, object> values = GetAllValues(),
                compare = o.GetAllValues();

            if (values.Count != compare.Count) return false;
            foreach (var key in values.Keys)
            {
                object value;
                if (!compare.TryGetValue(key, out value) 
                    || !value.Equals(values[key])) return false;
            }
            return true;
        }

        /// <summary>
        /// Loads a Dictionary of values into the object. Keys with corresponding column values are mapped and stored.
        /// </summary>
        /// <param name="values"></param>
        public virtual void LoadValues(Dictionary<string, object> values)
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                try {
                    var value = values[attribute.Name];
                    switch (value)
                    {
                    case (System.DBNull):
                        value = null;
                        break;
                    case (DateTime):
                        if (!typeof(string).IsInstanceOfType(value)) break;
                        value = SqlUtils.ParseDate(value);
                        break;
                    case (decimal):
                        value = Decimal.ToDouble((decimal) value);
                        break;
                    case (Int64):
                        value = Convert.ToInt32(value);
                        break;
                    }
                    p.SetValue(this, value);
                } catch (Exception e)
                {Console.WriteLine($"For property {p} of column {attribute.Name}: "); Console.WriteLine(e.Message); Console.WriteLine(e.StackTrace);}
            }
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

                values.Add($"[{attribute.Name}]");
            }

            return String.Join(",", values);
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
        /// Parses all columns with provided DataCleaners and sets their values to the result. 
        /// This method is called by default during Create() and Update() and may be turned off by setting validate:false
        /// </summary>
        public void Clean()
        {

            foreach (var p in GetColumns<ColumnAttribute> ())
            {
                try
                {
                    foreach (var a in p.GetCustomAttributes())
                    {
                        if (typeof(DataCleanerAttribute).IsInstanceOfType(a))
                        {
                            var val = p.GetValue(this);
                            p.SetValue(this, ((DataCleanerAttribute)a).Clean(p.Name, val, this));
                        }
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    throw new InvalidDataException($"For property {p}: {e.Message}");
                }
            }
            
        }

        /// <summary>
        /// Checks all columns against provided DataValidators and returns if the validation passed.
        /// This method is called by default during Create() and Update() and may be turned off by setting validate:false
        /// </summary>
        public void Validate()
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var c = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (c == null) continue;
                try
                {
                    foreach (var a in p.GetCustomAttributes())
                    {
                        if (typeof(DataValidatorAttribute).IsInstanceOfType(a))
                        {
                            var val = p.GetValue(this);
                            var validator = (DataValidatorAttribute) a;
                            if (validator.Validate(c.Name, val, this)) continue;

                            throw new InvalidDataException(validator.GetMessage());
                        }
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    throw new InvalidDataException($"For property {p.Name}: {e.Message}");
                }
            }
        }

        protected IEnumerable<PropertyInfo> GetColumns<T> () where T: Attribute
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var c = p.GetCustomAttribute(typeof(T));
                if (c == null) continue;
                yield return p;
            }
        }
    }
}
