using System;
using System.IO;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public class ConnectionInfo
    {
        private static string _connectionString;

        public ConnectionInfo(ConnectionInfoTypes type)
        {
            // type parameter kept for backward compatibility with existing test DataRow attributes
        }

        public string ConnectionString
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

    public enum ConnectionInfoTypes
    {
        Linux = 0
    }
}
