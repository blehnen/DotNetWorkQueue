using System;
using System.IO;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests
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
    }
}
