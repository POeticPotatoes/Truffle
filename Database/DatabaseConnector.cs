using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Truffle.Database
{
    /// <summary>
    /// <para> Holds a connection to an Sql database and provides methods to interact with it with queries. </para>
    /// <para> This class is disposable and should hence be instantiated with the "using" keyword, or by calling its Dispose() method after use. </para>
    /// </summary>
    public class DatabaseConnector : IDisposable
    {
        public static readonly int DefaultTimeout = 30;
        private static bool _verbose = false;
        private static int _timeout = 30;
        private static ILogger _logger;
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

        public static void setVerbose(bool verbose, ILogger logger)
        {
            _verbose = verbose;
            _logger = logger;
        }

        public static void setTimeout(int timeout)
        {
            _timeout = timeout;
        }

        public static int getTimeout()
        {
            return _timeout;
        }

        /// <summary>
        /// <para> Runs an Sql query or procedure and returns the result. </para>
        /// <para> This returns an object[] by default, but can be configured to return a List(Dictionary(string, object)) instead
        /// instead by setting complex=true </para>
        /// </summary>
        /// <param name="text">The query to run. If the query is a procedure, then the name of the procedure. </param>
        /// <param name="isProcedure">Whether the command is a procedure</param>
        /// <param name="values">The procedure parameters to be passed, if any</param>
        /// <param name="complex">Sets the return type of the method to a List(Dictionary(string, object)) if set to true</param>
        public object RunCommand(string text, bool isProcedure=false, Dictionary<string, object> values = null, bool complex=false) 
        {
            if (_verbose) _logger.LogInformation(text);
            using (var cmd = BuildSqlCommand(text, isProcedure, values))
            {
                cmd.CommandTimeout = _timeout;
                // Execute the command
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (complex) return DataCollector.ReadComplexValues(reader);
                    return DataCollector.ReadValues(reader);
                }
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
        public async Task<object> RunCommandAsync(string text, bool isProcedure=false, Dictionary<string, object> values = null, bool complex=false) 
        {
            if (_verbose)  _logger.LogInformation(text);
            using (var cmd = BuildSqlCommand(text, isProcedure, values))
            {
                cmd.CommandTimeout = _timeout;
                // Execute the command
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (complex) return DataCollector.ReadComplexValues(reader);
                    return DataCollector.ReadValues(reader);
                }
            }
        }

        private SqlCommand BuildSqlCommand(string text, bool isProcedure=false, Dictionary<string, object> values = null)
        {
            SqlCommand cmd = new SqlCommand(text, connection);
            if (isProcedure)
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (values != null)
                {
                    foreach (string s in values.Keys)
                        cmd.Parameters.Add(new SqlParameter(s, values[s]));
                } 
            }
            return cmd;
        }
    }
}