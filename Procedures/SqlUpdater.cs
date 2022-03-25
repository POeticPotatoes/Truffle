using System;
using System.Text;
using Truffle.Database;
using Truffle.Model;

namespace Truffle.Procedures
{
    /// <summary>
    /// A class that stores and calls updates on an sql table targeting a specific entry
    /// </summary>
    public class SqlUpdater : SqlEditor
    {
        private readonly string id, key;
        
        /// <summary>
        /// Creates an instance of an SqlUpdater that targets an entry given its unique identifier
        /// </summary>
        /// <param name="id">The value of the entry's identifier</param>
        /// <param name="idtype">The column name of the entry's identifier</param>
        public SqlUpdater(object id, string key)
        {
            this.key = key;
            if (id == null)
            {
                this.id = " IS NULL";
                return;
            }  
            this.id = $"={Parse(id)}";
        }

        public SqlUpdater(SqlObject o): base(o) {
            key = o.GetId();
            var raw = o.GetType().GetProperty(key).GetValue(o);
            id = Parse(raw);
        }

        /// <summary>
        /// Updates a table in a database based on the changes registered in this object. Will not update the table if no changes have been registered using Set()
        /// </summary>
        /// <param name="table">The table to be updated</param>
        /// <param name="database">The databse to be updated</param>
        /// <returns>Whether the update to the table was successful</returns>
        public Boolean Update(string table, DatabaseConnector database)
        {
            var fields = GetFields();
            if (fields.Count == 0) return true;
            StringBuilder text = new StringBuilder($"UPDATE {table} SET");

            foreach (string column in fields.Keys)
            {
                string value = fields[column];
                text.Append($" {column} = {value},");
            }
            text.Length--;
            text.Append($" WHERE {key}={id}");

            //Console.WriteLine(text);

            try {
                database.RunCommand(text.ToString());
                return true;
            
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}