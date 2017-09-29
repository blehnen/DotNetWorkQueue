using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Basic
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
            var builder = new SQLiteConnectionStringBuilder(connectionString); //will fail here if not valid
            return builder.DataSource;
        }
    }
}
