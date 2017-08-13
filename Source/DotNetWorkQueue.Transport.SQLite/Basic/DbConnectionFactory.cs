using System;
using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IDbConnectionFactory" />
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConnectionFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public DbConnectionFactory(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Creates a new connection to the database
        /// </summary>
        /// <returns></returns>
        public IDbConnection Create()
        {
            return new SQLiteConnection(_connectionInformation.ConnectionString);
        }
    }
}
