using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
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
