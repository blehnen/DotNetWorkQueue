namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <summary>
    /// Returns the 'source' value from a connection string
    /// </summary>
    public interface IDbDataSource
    {
        /// <summary>
        /// Returns the 'source' value from a connection string
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        string DataSource(string connectionString);
    }
}
