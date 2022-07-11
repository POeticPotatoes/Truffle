using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
        private PropertyInfo id;
        private Dictionary<string, PropertyInfo> columns = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Initialises an empty instance of an SqlObject
        /// </summary>
        public SqlObject() 
        {
            LoadProperties();
        }

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
            this.LoadProperties();
            InitFromDatabase(value, GetId(), database);
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
            this.LoadProperties();
            InitFromDatabase(value, column, database);
        }

        protected void InitFromDatabase(object value, string column, DatabaseConnector database)
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
            LoadProperties();
            LoadValues(values);
        }

        /// <summary>
        /// Builds a string that can be used to select all relevant columns and return all entries from a table.
        /// </summary>
        /// <returns>The generated string.</returns>
        public virtual string BuildAllRequest(int top = -1, string orderBy = null) 
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
            if (id == null)
                foreach (PropertyInfo p in columns.Values)
                    if (p.GetCustomAttribute(typeof(IdAttribute)) != null)
                    {
                        id = p;
                        break;
                    }
            if (id == null) return null;
            var a = (ColumnAttribute) id.GetCustomAttribute(typeof(ColumnAttribute));
            return a.Name;
        }

        public virtual PropertyInfo GetIdProperty()
        {
            if (id == null) GetId();
            if (id == null) return null;
            return id;
        }

        public virtual void SetId(object value)
        {
            if (id == null) GetId();
            if (id == null) return;
            id.SetValue(this, value);
        }

        public virtual object GetIdValue()
        {
            if (id == null) GetId();
            if (id == null) return null;
            return id.GetValue(this);
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
        public virtual Dictionary<string, object> GetAllValues(bool excludeOptional=false)
        {
            Dictionary<string, object> ans = new Dictionary<string, object>();

            foreach (KeyValuePair<string, PropertyInfo> c in columns)
            {
                PropertyInfo p = c.Value;
                if (excludeOptional && p.GetCustomAttribute(typeof(IdentityAttribute)) != null)
                        continue;

                var val = p.GetValue(this);
                if (excludeOptional && val == null && p.GetCustomAttribute(typeof(OptionalAttribute)) != null)
                    continue;
                ans.Add(c.Key, val);
            }
            return ans;
        }

        /// <summary>
        /// Logs all all keys, values and types stored in the object.
        /// </summary>
        public virtual void LogValues()
        {
            foreach (PropertyInfo p in columns.Values)
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
            var inserter = CreateEditor<SqlInserter>(validate);
            inserter.Insert(GetTable(),database);
        }

        /// <summary>
        /// Creates a new entry in a database with values stored in this object.
        /// Used when we want to create a new entry with a serial primary key
        /// </summary>
        /// <param name="database">The database to create a new entry in</param>
        /// <returns>The entry that was created with its serial priamry key</returns>
        public virtual object CreateWithOutput(DatabaseConnector database, bool validate=true) 
        {
            var inserter = CreateEditor<SqlInserter>(validate);
            return inserter.InsertWithOutput(GetTable(),database);
        }

        /// <summary>
        /// Creates a new entry in a database asynchronously with values stored in this object.
        /// </summary>
        /// <param name="database">The database to create a new entry in</param>
        public virtual Task CreateAsync(DatabaseConnector database, bool validate=true) 
        {
            var inserter = CreateEditor<SqlInserter>(validate);
            return inserter.InsertAsync(GetTable(),database);
        }

        /// <summary>
        /// Updates an existing entry in a database asynchronously with values stored in this object.
        /// </summary>
        /// <param name="database">The database to update</param>
        public virtual void Update(DatabaseConnector database, bool validate=true) 
        {
            var updater = CreateEditor<SqlUpdater>(validate);
            updater.Update(GetTable(),database);
        }

        /// <summary>
        /// Updates an existing entry in a database with values stored in this object.
        /// </summary>
        /// <param name="database">The database to update</param>
        public virtual Task UpdateAsync(DatabaseConnector database, bool validate=true) 
        {
            var updater = CreateEditor<SqlUpdater>(validate);
            return updater.UpdateAsync(GetTable(),database);
        }

        public virtual void Delete(DatabaseConnector database, int top=1, string orderby=null)
        {
            var selector = new SqlSelector().Set(GetId(), GetIdValue());
            database.RunCommand(selector.BuildDelete(GetTable(), top, orderby));
        }

        public virtual Task DeleteAsync(DatabaseConnector database, int top=1, string orderby=null)
        {
            var selector = new SqlSelector().Set(GetId(), GetIdValue());
            return database.RunCommandAsync(selector.BuildDelete(GetTable(), top, orderby));
        }
        public T CreateEditor<T>(bool validate = true) where T: SqlEditor
        {
            if (validate)
            {
                this.Clean();
                this.Validate();
            }
            return (T) Activator.CreateInstance(typeof(T), new object[] {this, false});
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

        private void LoadProperties()
        {
            foreach (var p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;
                
                columns.Add(attribute.Name, p);
            }
        }

        /// <summary>
        /// Loads a Dictionary of values into the object. Keys with corresponding column values are mapped and stored.
        /// </summary>
        /// <param name="values"></param>
        public virtual void LoadValues(Dictionary<string, object> values)
        {
            foreach (KeyValuePair<string, PropertyInfo> c in columns)
            {
                try {
                    var value = values[c.Key];
                    switch (value)
                    {
                    case (System.DBNull):
                        value = null;
                        break;
                    case (decimal):
                        value = Decimal.ToDouble((decimal) value);
                        break;
                    case (Int64):
                        value = Convert.ToInt32(value);
                        break;
                    case (Byte[]):
                        if (c.Value.PropertyType == typeof(byte))
                            break;
                        value = Convert.ToBase64String((byte[]) value);
                        break;
                    }
                    c.Value.SetValue(this, value);
                } catch (KeyNotFoundException e)
                {
                    Console.WriteLine($"For property {c.Value.Name} of column {c.Key}: "); 
                    Console.WriteLine(e.Message); 
                    Console.WriteLine(e.StackTrace);
                } catch (Exception e)
                {
                    Console.WriteLine($"For property {c.Value.Name} of column {c.Key}: "); 
                    Console.WriteLine(e.Message); 
                    Console.WriteLine(e.StackTrace);
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns the column list for a query to a database for this object.
        /// </summary>
        /// <returns></returns>
        public virtual string BuildColumnSelector()
        {
            return String.Join(",", columns.Keys);
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
            return $"SELECT TOP 1 {BuildColumnSelector()} FROM {GetTable()} WHERE [{column}]={val}";
        }

        /// <summary>
        /// Parses all columns with provided DataCleaners and sets their values to the result. 
        /// This method is called by default during Create() and Update() and may be turned off by setting validate:false
        /// </summary>
        public void Clean()
        {
            foreach (PropertyInfo p in columns.Values)
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
            foreach (KeyValuePair<string, PropertyInfo> c in columns)
            {
                PropertyInfo p = c.Value;
                try
                {
                    foreach (var a in p.GetCustomAttributes())
                    {
                        if (typeof(DataValidatorAttribute).IsInstanceOfType(a))
                        {
                            var val = p.GetValue(this);
                            var validator = (DataValidatorAttribute) a;
                            if (validator.Validate(c.Key, val, this)) continue;

                            throw new InvalidDataException(validator.GetMessage());
                        }
                    }
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    throw new InvalidDataException($"For property {p.Name}: {e.Message}");
                }
            }
        }

        public static string GetTable(Type t)
        {
            var a = (TableAttribute) t.GetCustomAttribute(typeof(TableAttribute));
            if (a == null) return null;
            return a.Name;
        }

        public bool HasColumn(string column)
        {
            return columns.ContainsKey(column);
        }
        
        /// <summary>
        /// Retrieves a value of a column from the object.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>The value of the column, or null if it isn't present.</returns>
        public virtual object GetValue(string column) 
        {
            if (!HasColumn(column))
                throw new KeyNotFoundException($"Column {column} does not exist in this model.");
            PropertyInfo i = columns[column];
            return i.GetValue(this);
        }

        public virtual void SetValue(string column, object value) 
        {
            if (!HasColumn(column))
                throw new KeyNotFoundException($"Column {column} does not exist in this model.");
            PropertyInfo i = columns[column];
            i.SetValue(this, value);
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

        public double GetDouble(string column)
        {
            object o = GetValue(column);
            return o==null?0:(double) o;
        }

        protected Dictionary<string, PropertyInfo> GetColumns()
        {
            return columns;
        }

        public List<string> GetColumnNames()
        {
            return new List<string>(columns.Keys);
        }
    }
}
