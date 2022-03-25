using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;

namespace Truffle.Database
{
    /// <summary>
    /// A Disposable class that connects to a database
    /// </summary>
    public class DatabaseConnector : IDisposable
    {
        private readonly SqlConnection connection;

        /// <summary>
        /// Constructor to connect to a database provided as an environment variable
        /// </summary>
        public DatabaseConnector(string connectionstr) 
        {
            connection = new SqlConnection(connectionstr);
            connection.Open();
        }

        /// <summary>
        /// Disposes of the database connection created
        /// </summary>
        public void Dispose() 
        {
            connection.Dispose();
        }

        /// <summary>
        /// Runs an sql query and returns the result
        /// </summary>
        /// <param name="text">The sql query to run</param>
        /// <returns>The result collected an array of objects</returns>
        public object RunCommand(string text, bool isProcedure=false, object[] values = null, bool complex=false) 
        {
            try {
                using (SqlCommand cmd = new SqlCommand(text, connection))
                {
                    if (isProcedure)
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        if (values != null)
                        {
                            object[] parameters = (object[]) RunCommand($"select PARAMETER_NAME from information_schema.parameters where specific_name='{text}'");
                            string[] keys = (from o in parameters select (string) o).ToArray();
                            for (var i=0;i<keys.Length;i++)
                                cmd.Parameters.Add(new SqlParameter(keys[i], values[i]));
                        } 
                    } 
                    // Execute the command
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (complex) return DataCollector.ReadComplexValues(reader);
                        return DataCollector.ReadValues(reader);
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                return null;
            }
        }

    }
}