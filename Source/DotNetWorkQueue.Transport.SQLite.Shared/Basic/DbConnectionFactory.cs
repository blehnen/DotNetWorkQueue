using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IDbConnectionFactory" />
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IDbFactory _dbFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbConnectionFactory" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="dbFactory">The database factory.</param>
        public DbConnectionFactory(IConnectionInformation connectionInformation,
            IDbFactory dbFactory)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => dbFactory, dbFactory);
            _connectionInformation = connectionInformation;
            _dbFactory = dbFactory;
        }
        /// <summary>
        /// Creates a new connection to the database
        /// </summary>
        /// <returns></returns>
        public IDbConnection Create()
        {
            return _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false);
        }
    }
}
