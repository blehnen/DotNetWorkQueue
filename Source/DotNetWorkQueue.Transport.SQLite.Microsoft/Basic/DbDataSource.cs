using DotNetWorkQueue.Transport.SQLite.Shared;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <summary>
    /// Returns the 'source' value from a connection string
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.SQLite.Shared.IDbDataSource" />
    public class DbDataSource : IDbDataSource
    {
        /// <inheritdoc />
        public string DataSource(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString); //will fail here if not valid
            return builder.DataSource;
        }
    }
}
