using System;
using System.Collections.Generic;
using Truffle.Database;

namespace Truffle.Model
{
    /// <summary>
    /// An extension of PartialSqlObject that can have dynamically defined table names or key columns.
    /// This class can be instantiated without extension.
    /// </summary>
    public class GenericSqlObject: PartialSqlObject
    {
        public string Id {get;set;}
        public string Table {get;set;}

        public string[] Columns {get;set;}

        /// <summary>
        /// Initialises an empty instance of a GenericSqlObject
        /// </summary>
        public GenericSqlObject() {}

        /// <summary>
        /// Initialises an empty instance of a GenericSqlObject, with table and key column corresponding to the object defined
        /// </summary>
        public GenericSqlObject(string table, string id)
        {
            this.Id = id;
            this.Table = table;
        }

        /// <summary>
        /// Initialises a new instance of a GenericSqlObject and fills it with values from a row in a database.
        /// The row is selected by provided key column and value.
        /// 
        /// The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {id}={value}</code>
        /// </summary>
        /// <param name="table">The table of the object</param>
        /// <param name="id">The name of the key column</param>
        /// <param name="value">The value of the key column</param>
        /// <param name="database">The database to connect to</param>
        public GenericSqlObject(string table, string id, object value, DatabaseConnector database)
        {
            this.Id = id;
            this.Table = table;
            base.InitFromDatabase(value, id, database);
        }

        /// <summary>
        /// Initialises a new instance of a GenericSqlObject and fills it with values from a row in a database.
        /// The row is selected by provided column and value, and the first entry found is taken.
        /// 
        /// The query is generated with BuildRequest() and would be similar to this: <code> select * from {table} where {column}={value}</code>
        /// </summary>
        /// <param name="table">The table of the object</param>
        /// <param name="id">The name of the key column</param>
        /// <param name="value">The value to find</param>
        /// <param name="column">The column to find the value in</param>
        /// <param name="database">The database to connect to</param>
        public GenericSqlObject(string table, string id, object value, string column, DatabaseConnector database)
        {
            this.Id = id;
            this.Table = table;
            base.InitFromDatabase(value, column, database);
        }

        /// <summary>
        /// Initialises a new instance of a GenericSqlObject and fills it with values from a Dictionary
        /// </summary>
        /// <param name="table">The table name</param>
        /// <param name="id">The name of the key column</param>
        /// <param name="values">The values to be stored and accessed in the GenericSqlObject</param>
        public GenericSqlObject(string table, string id, Dictionary<string, object> values)
        {
            this.Id = id;
            this.Table = table;
            LoadValues(values);
        }

        public override string GetId()
        {
            return Id;
        }

        public override string GetTable()
        {
            return Table;
        }

        public override string BuildColumnSelector()
        {
            if (Columns == null || Columns.Length == 0) return "*";
            return String.Join(',', Columns);
        }
    }
}