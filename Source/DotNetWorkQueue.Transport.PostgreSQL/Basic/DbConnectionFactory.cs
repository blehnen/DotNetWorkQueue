using DotNetWorkQueue.Validation;
using Npgsql;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IDbConnectionFactory" />
    public class DbConnectionFactory : RelationalDatabase.IDbConnectionFactory
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
            return new NpgsqlConnection(_connectionInformation.ConnectionString);
        }
    }
}
