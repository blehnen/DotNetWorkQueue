using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// This object holds the state of an item that is in progress. It's used to commit or rollback the work item as needed.
    /// </summary>
    internal class ConnectionHolder : IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>
    {
        #region Member level variables
        private int _disposeCount;
        private NpgsqlConnection _npgsqlConnection;
        private NpgsqlTransaction _npgsqlTransaction;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionHolder(IConnectionInformation connectionInfo,
            PostgreSqlMessageQueueTransportOptions options)
        {
            _npgsqlConnection = new NpgsqlConnection(connectionInfo.ConnectionString);
            _npgsqlConnection.Open();

            if (options.EnableHoldTransactionUntilMessageCommitted)
            {
                _npgsqlTransaction = _npgsqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The current server connection. May be null if instance is disposed
        /// </summary>
        /// <value>
        /// The SQL connection.
        /// </value>
        public NpgsqlConnection Connection
        {
            get
            {
                ThrowIfDisposed(); 
                return _npgsqlConnection; 
            }
            set
            {
                ThrowIfDisposed();
                _npgsqlConnection = value;
            }
        }

        /// <summary>
        /// The current SQL transaction. May be null. Will only be non-null if transactions are being held for the life of the unit of work.
        /// </summary>
        /// <value>
        /// The SQL transaction.
        /// </value>
        public NpgsqlTransaction Transaction
        {
            get
            {
                ThrowIfDisposed();
                return _npgsqlTransaction;
            }
            set
            {
                ThrowIfDisposed();
                _npgsqlTransaction = value;
            }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public NpgsqlCommand CreateCommand()
        {
            if(_npgsqlConnection == null)
            {
                throw new DotNetWorkQueueException("An attempt was made to create a SQL command object, but the SQL connection is null");
            }
            var npgsqlCommand = _npgsqlConnection.CreateCommand();
            if (_npgsqlTransaction != null)
            {
                npgsqlCommand.Transaction = _npgsqlTransaction;
            }
            return npgsqlCommand;
        }

        #region Dispose
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            if (_npgsqlTransaction != null)
            {
                _npgsqlTransaction.Dispose();
                _npgsqlTransaction = null;
            }

            if (_npgsqlConnection == null) return;

            _npgsqlConnection.Dispose();
            _npgsqlConnection = null;
        }
        /// <inheritdoc />
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        private void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }

        #endregion

        #endregion
    }
}
