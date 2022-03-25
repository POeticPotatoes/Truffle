using System;
using System.Collections.Generic;
using System.Reflection;
using Success.Utils;
using Truffle.Database;

namespace Truffle.Model
{
    public class PartialSqlObject : SqlObject
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();

        public PartialSqlObject(): base() {}

        public PartialSqlObject(Dictionary<string, object> values): base(values)
        {
            Data = values;
        }

        public PartialSqlObject(object val, DatabaseConnector database)
        {
            string req = BuildRequest(val, GetId());

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{val} was not present in the database");

            LoadValues(response[0]);
            Data  = response[0];
        }

        public PartialSqlObject(object val, string key, DatabaseConnector database)
        {
            string req = BuildRequest(val, key);

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{key} of value {SqlUtils.Parse(val)} was not present in the database");

            LoadValues(response[0]);
            Data  = response[0];
        }

        public object GetValue(string key) 
        {
            return Data[key];
        }

        public override Dictionary<string, object> GetAllValues()
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                Data[attribute.Name] =  p.GetValue(this);
            }

            return Data;
        }

        /// <summary>
        /// Returns the value of a parameter cast as a Boolean
        /// </summary>
        /// <param name="key">The name of the parameter</param>
        /// <returns>The Boolean value of the parameter, or null if it isn't present</returns>
        public Boolean GetBoolean(string key) 
        {
            return (Boolean) GetValue(key);
        }

        /// <summary>
        /// Returns the value of a parameter cast as a string
        /// </summary>
        /// <param name="key">The name of the parameter</param>
        /// <returns>The string value of the parameter, or null if it isn't present</returns>
        public string GetString(String key) 
        {
            return (string) GetValue(key);
        }

        /// <summary>
        /// Returns the value of a parameter cast as a Date
        /// </summary>
        /// <param name="key">The name of the parameter</param>
        /// <returns>The Date value of the parameter, or null if it isn't present</returns>
        public DateTime? GetDate(String key) 
        {
            return (DateTime?) GetValue(key);
        }

        /// <summary>
        /// Returns the value of a parameter cast as an int
        /// </summary>
        /// <param name="key">The name of the parameter</param>
        /// <returns>The int value of the parameter, or null if it isn't present</returns>
        public int GetInt(String key) 
        {
            object o = GetValue(key);
            if (o == null) return 0;
            return (int) o;
        }

        protected override string BuildRequest(object raw, string key)
        {
            string val = SqlUtils.Parse(raw);
            return $"SELECT * FROM {GetTable()} WHERE {key}={val}";
        }
    }
}