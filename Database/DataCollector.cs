using System.Collections.Generic;
using System.Data.SqlClient;

namespace Truffle.Database
{
    /// <summary>
    /// A static class that provides utility for reading sql data
    /// </summary>
    public static class DataCollector
    {
        /// <summary>
        /// Collects the values from an SqlDataReader and returns them as an array
        /// </summary>
        /// <param name="reader">The SqlDataReader to be parsed</param>
        /// <returns>The resulting array</returns>
        public static object[] ReadValues(SqlDataReader reader) 
        {
            List<object> response = new List<object>();

            while (reader.Read())
            {
                // Read every line of data
                for (var i=0;i<reader.FieldCount;i++) 
                {
                    response.Add(reader.GetValue(i));
                }
            }

            // Call Close when done reading.
            reader.Close();
            return response.ToArray();
        }

        public static List<Dictionary<string, object>> ReadComplexValues(SqlDataReader reader)
        {
            List<Dictionary<string, object>> response = new List<Dictionary<string, object>>();

            while (reader.Read())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                for (var i=0;i<reader.FieldCount;i++) 
                {
                    string name = reader.GetName(i);
                    object value = reader.GetValue(i);
                    dict.Add(name, value);
                }
                response.Add(dict);
            }

            // Call Close when done reading.
            reader.Close();
            return response;
        }
    }
}