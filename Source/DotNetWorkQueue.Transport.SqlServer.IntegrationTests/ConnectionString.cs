using System;
using System.IO;
using System.Net.NetworkInformation;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests
{
    public static class ConnectionInfo
    {
        private static string _connectionString;
        /// <summary>
        /// The connection string to the SQL DB for the integration tests. All tests in this project will use this connection string
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(_connectionString))
                    return _connectionString;

                var connectionString = File.ReadAllText("connectionstring.txt");
                _connectionString = connectionString;

                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new NullReferenceException("connectionstring.txt is missing or contains no data");
                }

                return connectionString;
            }
        }
        /// <summary>
        /// The schema default
        /// </summary>
        public static string SchemaDefault = "dbo";
        /// <summary>
        /// An existing schema for testing
        /// </summary>
        public static string Schema1 = "test1";
        /// <summary>
        /// An existing schema for testing
        /// </summary>
        public static string Schema2 = "test2";
    }
}
