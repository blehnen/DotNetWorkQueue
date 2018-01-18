namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public class ConnectionInfo
    {
        /// <summary>
        /// The connection string to the redis server for the integration tests. All tests in this project will use this connection string for linux tests
        /// </summary>
        private const string ConnectionStringLinux = "192.168.0.212,defaultDatabase=1,syncTimeout=15000";
        /// <summary>
        /// The connection string to the redis server for the integration tests. All tests in this project will use this connection string for windows tests
        /// </summary>
        private const string ConnectionStringWindows = "V-WINRedis,defaultDatabase=1,syncTimeout=15000";
        private readonly ConnectionInfoTypes _type;
        public ConnectionInfo(ConnectionInfoTypes type)
        {
            _type = type;
        }

        public string ConnectionString
        {
            get
            {
                switch (_type)
                {
                    case ConnectionInfoTypes.Linux:
                        return ConnectionStringLinux;
                    case ConnectionInfoTypes.Windows:
                        return ConnectionStringWindows;
                    default:
                        return string.Empty;
                }
            }
        }
    }

    public enum ConnectionInfoTypes
    {
        Linux = 0,
        Windows = 1
    }
}
