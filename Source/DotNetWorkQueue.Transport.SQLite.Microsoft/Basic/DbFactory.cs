using System.Data;
using DotNetWorkQueue.Transport.SQLite.Shared;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    public class DbFactory: IDbFactory
    {
        private readonly IContainer _container;
        public DbFactory(IContainerFactory container)
        {
            _container = container.Create();
        }

        public IDbConnection CreateConnection(string connectionString, bool forMemoryHold)
        {
            return new SingleConnection(connectionString, forMemoryHold);
        }

        public IDbCommand CreateCommand(IDbConnection connection)
        {
            return connection.CreateCommand();
        }

        public ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection)
        {
            var transaction = _container.GetInstance<ISQLiteTransactionWrapper>();
            transaction.Connection = connection;
            return transaction;
        }
    }
}
