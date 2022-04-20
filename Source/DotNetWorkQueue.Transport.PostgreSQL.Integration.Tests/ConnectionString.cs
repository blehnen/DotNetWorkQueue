namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests
{
    public static class ConnectionInfo
    {
        /// <summary>
        /// The connection string to the SQL DB for the integration tests. All tests in this project will use this connection string
        /// </summary>
        public static string ConnectionString =
            "Server=192.168.0.58;Port=5432;Database=integrationtesting;Integrated Security=true;Maximum Pool Size=250;userid=brian";
    }
}
