using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
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
        /// <inheritdoc />
        public IDbConnection Create()
        {
            return new NpgsqlConnection(_connectionInformation.ConnectionString);
        }
    }
}
