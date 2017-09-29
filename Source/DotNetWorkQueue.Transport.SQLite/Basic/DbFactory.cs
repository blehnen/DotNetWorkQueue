using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Creates new db objects
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.SQLite.Shared.IDbFactory" />
    public class DbFactory: IDbFactory
    {
        private readonly IContainer _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="DbFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DbFactory(IContainerFactory container)
        {
            _container = container.Create();
        }

        /// <inheritdoc />
        public IDbConnection CreateConnection(string connectionString, bool forMemoryHold)
        {
            return new SQLiteConnection(connectionString);
        }

        /// <inheritdoc />
        public IDbCommand CreateCommand(IDbConnection connection)
        {
            return connection.CreateCommand();
        }

        /// <inheritdoc />
        public ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection)
        {
            var transaction = _container.GetInstance<ISQLiteTransactionWrapper>();
            transaction.Connection = connection;
            return transaction;
        }
    }
}
