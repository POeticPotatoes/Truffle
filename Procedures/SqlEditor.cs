using System.Collections.Generic;
using Truffle.Model;
using Truffle.Utils;

namespace Trufle.Procedures
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
        public SqlEditor(SqlObject o, bool validate=true)
        {
            if (validate)
            {
                o.Clean();
                o.Validate();
            }
            fields = new Dictionary<string, string>();
            Dictionary<string, object> values = o.GetAllValues(ignoreIdentities: true);
            foreach (string column in values.Keys)
                Set(column, values[column]);
        }
        
        /// <summary>
        /// Saves a key value pair corresponding to a column in a table.
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <param name="value">The value of the column</param>
        public virtual void Set(string column, object value)
        {
            fields[column] = SqlUtils.Parse(value);
        }

        /// <summary>
        /// Saves all keys and values from a Dictionary.
        /// </summary>
        /// <param name="toAdd">The Dictionary to add</param>
        public void SetAll(Dictionary<string, object> toAdd)
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
    }
}