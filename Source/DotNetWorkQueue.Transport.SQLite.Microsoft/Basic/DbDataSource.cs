using DotNetWorkQueue.Transport.SQLite.Shared;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
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
