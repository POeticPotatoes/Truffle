using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truffle.Database;
using Truffle.Model;
using Trufle.Procedures;

namespace Truffle.Procedures
{
    /// <summary>
    /// Provides utility in inserting an entry into a database.
    /// </summary>
    public class SqlInserter : SqlEditor
    {
        private SqlObject sqlObject;

        /// <summary>
        /// Initialises an empty SqlInserter.
        /// </summary>
        public SqlInserter() : base() {}

        /// <summary>
        /// Initialises an SqlEditor with values in an SqlObject.
        /// A subsequent Insert() call would create a new entry in the database with values from the object.
        /// </summary>
        /// <param name="o">The SqlObject to be created</param>
        public SqlInserter(SqlObject o, bool validate=true) : base(o, validate) 
        {
            this.sqlObject = o;
        }

        /// <summary>
        /// Inserts a new entry to the database in the given table, with values stored in this object.
        /// If no values are present, no entry is created.
        /// </summary>
        /// <param name="table">The table to be inserted to</param>
        /// <param name="database">The database to use</param>
        public void Insert(string table, DatabaseConnector database)
        {
            string command = BuildCommand(table);
            if (command == null) return;
            database.RunCommand(command);
            return;
        }

        /// <summary>
        /// Inserts a new entry to the database in the given table, with values stored in this object.
        /// Mainly used when we are inserting into a table with serial primary keys
        /// If no values are present, no entry is created.
        /// </summary>
        /// <param name="table">The table to be inserted to</param>
        /// <param name="database">The database to use</param>
        /// <returns> The object that was inserted including its serial primary key </returns>
        public object InsertWithOutput(string table, DatabaseConnector database)
        {
            string command = BuildCommand(table, true);
            if (command == null) return null;
            List<Dictionary<string, object>> result = 
                (List<Dictionary<string, object>>) database.RunCommand(command, complex: true);
            Dictionary<string, object> inserted = result[0];
            return inserted;
        }

        /// <summary>
        /// Inserts a new entry to the database in the given table asynchronously, with values stored in this object.
        /// If no values are present, no entry is created.
        /// </summary>
        /// <param name="table">The table to be inserted to</param>
        /// <param name="database">The database to use</param>
        public async Task InsertAsync(string table, DatabaseConnector database)
        {
            string command = BuildCommand(table);
            if (command == null) return;
            await database.RunCommandAsync(command);
        }

        private string BuildCommand(string table, bool output = false)
        {
            Dictionary<string, string> fields = GetFields();
            if (fields.Count == 0) return null;
            StringBuilder cmd = new StringBuilder(
                    $"insert {table} ({String.Join(',', fields.Keys)})");
            if (output) 
                cmd.Append($" output {BuildOutputCommand()}");
            cmd.Append($" values ({String.Join(',',fields.Values)})");
            return cmd.ToString();
        }

        private string BuildOutputCommand()
        {
            List<string> columns;
            if (sqlObject != null)
                columns = sqlObject.GetColumnNames();
            else
                columns = new List<string>(GetFields().Keys);

            StringBuilder cmd = new StringBuilder();
            foreach (string col in columns)
                cmd.Append($"inserted.[{col}], ");

            cmd.Length -= 2;
            return cmd.ToString();
        }
    }
}
