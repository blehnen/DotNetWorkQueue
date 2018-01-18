using System.Data;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    internal class SqLiteTransactionWrapper : ISQLiteTransactionWrapper
    {
        public IDbConnection Connection
        {
            get;
            set;
        }

        public IDbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }
    }
}
