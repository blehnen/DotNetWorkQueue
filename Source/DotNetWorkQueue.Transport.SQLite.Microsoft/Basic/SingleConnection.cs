using System.Data;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    public class SingleConnection: IDbConnection
    {
        private readonly SqliteConnection _connection;
        private readonly bool _forMemoryHold;
        private static readonly object Locker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="forMemoryHold">if set to <c>true</c> [for memory hold].</param>
        public SingleConnection(string connectionString, bool forMemoryHold)
        {
            _forMemoryHold = forMemoryHold;

            if (!_forMemoryHold)
            {
                Monitor.Enter(Locker);
            }
            _connection = new SqliteConnection(connectionString);
        }

        public void Dispose()
        {
            _connection.Dispose();
            if (!_forMemoryHold)
            {
                Monitor.Exit(Locker);
            }
        }

        public IDbTransaction BeginTransaction()
        {
            return ((IDbConnection) _connection).BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return ((IDbConnection) _connection).BeginTransaction(il);
        }

        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            _connection.Close();
        }

        public IDbCommand CreateCommand()
        {
            return ((IDbConnection) _connection).CreateCommand();
        }

        public void Open()
        {
            _connection.Open();
        }

        public string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        public int ConnectionTimeout => _connection.ConnectionTimeout;

        public string Database => _connection.Database;

        public ConnectionState State => _connection.State;
    }
}
