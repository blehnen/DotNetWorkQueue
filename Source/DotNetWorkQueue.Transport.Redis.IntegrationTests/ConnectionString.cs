using System;
using System.IO;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public static class ConnectionInfo
    {
        private static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(_connectionString))
                    return _connectionString;

                var connectionString = File.ReadAllText("connectionstring.txt");
                _connectionString = connectionString.Trim();

                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new NullReferenceException("connectionstring.txt is missing or contains no data");
                }

                return _connectionString;
            }
        }
    }
}
