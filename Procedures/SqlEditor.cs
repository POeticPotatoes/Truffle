using System;
using System.Collections.Generic;
using Success.Utils;
using Truffle.Model;

namespace Truffle.Procedures
{
    /// <summary>
    /// An abstract class that provides methods to store key value pairs for subsequent actions to an Sql database.
    /// </summary>
    public abstract class SqlEditor
    {
        private readonly Dictionary<string, string> fields = new Dictionary<string, string>();

        /// <summary>
        /// Initialises an empty SqlEditor.
        /// </summary>
        public SqlEditor() {}

        /// <summary>
        /// Initialises an SqlEditor with values in an SqlObject. 
        /// All values from the object are stored and can be retrieved later.
        /// </summary>
        /// <param name="o">The SqlObject</param>
        public SqlEditor(SqlObject o)
        {
            fields = new Dictionary<string, string>();
            Dictionary<string, object> values = o.GetAllValues();
            foreach (string column in values.Keys)
            {
                string result = Parse(values[column]);
                fields.Add(column, result);
            }
        }
        
        /// <summary>
        /// Saves a key value pair corresponding to a column in a table.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <param name="value">The value of the column</param>
        public virtual void Set(string column, object value)
        {
            fields[column] = Parse(value);
        }

        /// <summary>
        /// Saves all keys and values from a Dictionary.
        /// </summary>
        /// <param name="toAdd">The Dictionary to add</param>
        public virtual void SetAll(Dictionary<string, object> toAdd)
        {
            foreach (string column in toAdd.Keys)
            {
                Set(column, toAdd[column]);
            }
        }

        /// <summary>
        /// Retrieves all saved fields from the editor.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, string> GetFields()
        {
            return fields;
        }

        /// <summary>
        /// Parses an object into a compatible format for a subsequent Sql query.
        /// By default, this relies on the SqlUtils.Parse() method.
        /// </summary>
        /// <param name="o">The object to be parsed</param>
        /// <returns>The formatted string</returns>
        protected virtual string Parse(object o)
        {
            return SqlUtils.Parse(o);
        }
    }
}