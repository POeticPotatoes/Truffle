using System;
using System.Collections.Generic;
using Truffle.Database;
using Truffle.Model;

namespace Truffle.Procedures
{
    /// <summary>
    /// Provides utility in inserting an entry into a database.
    /// </summary>
    public class SqlInserter : SqlEditor
    {
        /// <summary>
        /// Initialises an empty SqlInserter.
        /// </summary>
        public SqlInserter() : base() {}

        /// <summary>
        /// Initialises an SqlEditor with values in an SqlObject.
        /// A subsequent Insert() call would create a new entry in the database with values from the object.
        /// </summary>
        /// <param name="o">The SqlObject to be created</param>
        public SqlInserter(SqlObject o) : base(o) {}

        /// <summary>
        /// Inserts a new entry to the database in the given table, with values stored in this object.
        /// If no values are present, no entry is created.
        /// </summary>
        /// <param name="table">The table to be inserted to</param>
        /// <param name="database">The database to use</param>
        public void Insert(string table, DatabaseConnector database)
        {
            Dictionary<string, string> fields = GetFields();
            if (fields.Count == 0) return;

            string command = $"insert {table} ({String.Join(',', fields.Keys)}) values ({String.Join(',',fields.Values)})";
            database.RunCommand(command);
            return;
        }
    }
}