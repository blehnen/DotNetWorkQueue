using System;
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <summary>
    /// Determines the full path and file name of a Sqlite DB, based on the connection string.
    /// </summary>
    public class GetFileNameFromConnectionString: IGetFileNameFromConnectionString
    {
        /// <summary>
        /// Gets the full path and file name of a DB. In memory databases will instead set the <seealso cref="ConnectionStringInfo.IsInMemory"/> flag to true.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public ConnectionStringInfo GetFileName(string connectionString)
        {
            SqliteConnectionStringBuilder builder;
            try
            {
                 builder = new SqliteConnectionStringBuilder(connectionString);
            }
            // ReSharper disable once UncatchableException
            catch (ArgumentException) //bad format - return a connection string info that isn't valid
            {
                return new ConnectionStringInfo(false, string.Empty);
            }

            var dataSource = builder.DataSource.ToLowerInvariant();
            var inMemory = dataSource.Contains(":memory:") || dataSource.Contains("mode=memory");

            if (inMemory || string.IsNullOrWhiteSpace(builder.ConnectionString))
                return new ConnectionStringInfo(inMemory, builder.DataSource);

            var uri = builder.ConnectionString.ToLowerInvariant();
            inMemory = uri.Contains(":memory:") || uri.Contains("mode=memory");

            return new ConnectionStringInfo(inMemory, builder.DataSource);
        }
    }
}
