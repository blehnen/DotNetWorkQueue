namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests
{
    public static class ConnectionInfo
    {
        /// <summary>
        /// The connection string to the SQL DB for the integration tests. All tests in this project will use this connection string
        /// </summary>
        public static string ConnectionString =
            "Server=V-SQL;Application Name=IntegrationTesting;Database=IntegrationTests;Trusted_Connection=True;max pool size=500";
    }
}
