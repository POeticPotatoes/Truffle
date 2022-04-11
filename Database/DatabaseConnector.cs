using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Truffle.Database
{
    /// <summary>
    /// <para> Holds a connection to an Sql database and provides methods to interact with it with queries. </para>
    /// <para> This class is disposable and should hence be instantiated with the "using" keyword, or by calling its Dispose() method after use. </para>
    /// </summary>
    public class DatabaseConnector : IDisposable
    {
        private readonly SqlConnection connection;

        /// <summary>
        /// Initialises a new instance of DatabaseConnector when provided with a connection string to an Sql database.
        /// </summary>
        public DatabaseConnector(string connectionstr) 
        {
            connection = new SqlConnection(connectionstr);
            connection.Open();
        }

        /// <summary>
        /// Disposes of the database connection created and all its resources
        /// </summary>
        public void Dispose() 
        {
            connection.Dispose();
        }

        /// <summary>
        /// <para> Runs an Sql query or procedure and returns the result. </para>
        /// <para> This returns an object[] by default, but can be configured to return a List(Dictionary(string, object)) instead
        /// instead by setting complex=true </para>
        /// </summary>
        /// <param name="text">The query to run. If the query is a procedure, then the name of the procedure. </param>
        /// <param name="isProcedure">Whether the command is a procedure</param>
        /// <param name="values">The procedure parameters to be passed, if any</param>
        /// <param name="complex">If set to true, returns a List(Dictionary(string, object))</param>
        public object RunCommand(string text, bool isProcedure=false, object[] values = null, bool complex=false) 
        {
            try {
                using (var cmd = BuildSqlCommand(text, isProcedure, values))
                {
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

        /// <summary>
        /// <para> Runs an Sql query or procedure asynchronously and returns the result. </para>
        /// <para> This returns an object[] by default, but can be configured to return a List(Dictionary(string, object)) instead
        /// instead by setting complex=true </para>
        /// </summary>
        /// <param name="text">The query to run. If the query is a procedure, then the name of the procedure. </param>
        /// <param name="isProcedure">Whether the command is a procedure</param>
        /// <param name="values">The procedure parameters to be passed, if any</param>
        /// <param name="complex">If set to true, returns a List(Dictionary(string, object))</param>
        public async Task<object> RunCommandAsync(string text, bool isProcedure=false, object[] values = null, bool complex=false) 
        {
            try {
                using (var cmd = BuildSqlCommand(text, isProcedure, values))
                {
                    // Execute the command
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
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

        private SqlCommand BuildSqlCommand(string text, bool isProcedure=false, object[] values = null)
        {
            SqlCommand cmd = new SqlCommand(text, connection);
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
            return cmd;
        }
    }
}