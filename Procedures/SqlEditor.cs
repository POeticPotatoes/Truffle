using System;
using System.Collections.Generic;
using Success.Utils;
using Truffle.Model;

namespace Truffle.Procedures
{
    public abstract class SqlEditor
    {
        private readonly Dictionary<string, string> fields = new Dictionary<string, string>();

        public SqlEditor() {}
        public SqlEditor(SqlObject o)
        {
            fields = new Dictionary<string, string>();
            Dictionary<string, object> values = o.GetAllValues();
            foreach (string column in values.Keys)
            {
                string result = SqlUtils.Parse(values[column]);
                fields.Add(column, result);
            }
        }
        
        public void Set(string column, object value)
        {
            fields[column] = SqlUtils.Parse(value);
        }

        public void SetAll(Dictionary<string, object> toAdd)
        {
            foreach (string column in toAdd.Keys)
            {
                fields[column] = SqlUtils.Parse(toAdd[column]);
            }
        }

        protected Dictionary<string, string> GetFields()
        {
            return fields;
        }
    }
}