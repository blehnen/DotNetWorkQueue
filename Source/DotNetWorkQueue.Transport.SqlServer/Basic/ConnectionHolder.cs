// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// This object holds the state of an item that is in progress. It's used to commit or rollback the work item as needed.
    /// </summary>
    internal class ConnectionHolder : IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>
    {
        #region Member level variables
        private int _disposeCount;
        private SqlConnection _sqlConnection;
        private SqlTransaction _sqlTransaction;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHolder" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionHolder(IConnectionInformation connectionInfo,
            SqlServerMessageQueueTransportOptions options)
        {
            _sqlConnection = new SqlConnection(connectionInfo.ConnectionString);
            _sqlConnection.Open();

            if (options.EnableHoldTransactionUntilMessageCommitted)
            {
                _sqlTransaction = _sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The current SQL server connection. May be null if instance is disposed
        /// </summary>
        /// <value>
        /// The SQL connection.
        /// </value>
        public SqlConnection Connection
        {
            get
            {
                ThrowIfDisposed(); 
                return _sqlConnection; 
            }
            set
            {
                ThrowIfDisposed();
                _sqlConnection = value;
            }
        }

        /// <summary>
        /// The current SQL transaction. May be null. Will only be non-null if transactions are being held for the life of the unit of work.
        /// </summary>
        /// <value>
        /// The SQL transaction.
        /// </value>
        public SqlTransaction Transaction
        {
            get
            {
                ThrowIfDisposed();
                return _sqlTransaction;
            }
            set
            {
                ThrowIfDisposed();
                _sqlTransaction = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a SQL command object from the current connection and sets the transaction if one is present.
        /// </summary>
        /// <returns></returns>
        public SqlCommand CreateCommand()
        {
            if(_sqlConnection == null)
            {
                throw new DotNetWorkQueueException("An attempt was made to create a SQL command object, but the SQL connection is null");
            }
            var sqlCommand = _sqlConnection.CreateCommand();
            if (_sqlTransaction != null)
            {
                sqlCommand.Transaction = _sqlTransaction;
            }
            return sqlCommand;
        }

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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

            if (_sqlTransaction != null)
            {
                _sqlTransaction.Dispose();
                _sqlTransaction = null;
            }

            if (_sqlConnection == null) return;

            _sqlConnection.Dispose();
            _sqlConnection = null;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
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
