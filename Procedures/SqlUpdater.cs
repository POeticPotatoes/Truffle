using System.Text;
using System.Threading.Tasks;
using Truffle.Database;
using Truffle.Model;
using Truffle.Utils;
using Trufle.Procedures;
using System;

namespace Truffle.Procedures
{
    /// <summary>
    /// Provides utility in updating an existing entry in a database.
    /// </summary>
    public class SqlUpdater : SqlEditor
    {
        private readonly string selector;
        
        /// <summary>
        /// Initialises a new SqlUpdater that targets a specific entry with a key-value pair corresponding to a column
        /// </summary>
        /// <param name="value">The value of the column</param>
        /// <param name="idtype">The name of the column</param>
        public SqlUpdater(object value, string column)
        {
            string id = SqlUtils.ParseSelector(value);
            selector = $"{column}{id}";
        }

        /// <summary>
        /// Initialises an SqlEditor with values in an SqlObject.
        /// A subsequent Update() call would update an entry in the database with values from the object.
        /// </summary>
        /// <param name="o">The SqlObject to be updated</param>
        public SqlUpdater(SqlObject o, bool validate=true): base(o, validate) {
            string key = o.GetId();
            var raw = o.GetType().GetProperty(key).GetValue(o);
            string id = SqlUtils.Parse(raw);
            selector = $"{key}={id}";
        }

        /// <summary>
        /// Initialises an SqlEditor with an SqlSelector containing parameters.
        /// A subsequent Update() call would update all entries in a database with values that fit its parameters.
        /// </summary>
        /// <param name="o">The SqlSelector with select parameters</param>
        public SqlUpdater(SqlSelector selector)
        {
            this.selector = selector.BuildParameters();
        }

        /// <summary>
        /// Updates a table in a database based on the changes registered in this object. Will not update the table if no changes have been registered using Set()
        /// </summary>if (value.GetType().Name == "Int64") 
        /// <param name="table">The table to be updated</param>
        /// <param name="database">The database to be updated</param>
        /// <returns>Whether the update to the table was successful</returns>
        public void Update(string table, DatabaseConnector database)
        {
            string cmd = BuildCommand(table);
            if (cmd == null) return;

            //Console.WriteLine(text);
            database.RunCommand(cmd.ToString());
        }

        /// <summary>
        /// Updates a table in a database asynchronously based on the changes registered in this object. Will not update the table if no changes have been registered using Set()
        /// </summary>
        /// <param name="table">The table to be updated</param>
        /// <param name="database">The database to be updated</param>
        /// <returns>Whether the update to the table was successful</returns>
        public async Task UpdateAsync(string table, DatabaseConnector database)
        {
            string cmd = BuildCommand(table);
            if (cmd == null) return;

            //Console.WriteLine(text);
            await database.RunCommandAsync(cmd);
        }

        private string BuildCommand(string table)
        {
            var fields = GetFields();
            if (fields.Count == 0) return null;
            StringBuilder text = new StringBuilder($"UPDATE {table} SET");

            foreach (string column in fields.Keys)
            {
                string value = fields[column];
                text.Append($" {column} = {value},");
            }
            text.Length--;
            text.Append($" WHERE {selector}");
            return text.ToString();
        }
    }
}