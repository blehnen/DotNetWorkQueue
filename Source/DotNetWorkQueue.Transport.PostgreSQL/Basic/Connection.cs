// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// This object holds the state of an item that is in progress. It's used to commit or rollback the work item as needed.
    /// </summary>
    internal class Connection : IDisposable, IIsDisposed
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
        public Connection(IConnectionInformation connectionInfo,
            PostgreSqlMessageQueueTransportOptions options)
        {
            NpgsqlConnection = new NpgsqlConnection(connectionInfo.ConnectionString);
            NpgsqlConnection.Open();

            if (options.EnableHoldTransactionUntilMessageCommited)
            {
                NpgsqlTransaction = NpgsqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
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
        public NpgsqlConnection NpgsqlConnection
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
        public NpgsqlTransaction NpgsqlTransaction
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
        /// <summary>
        /// Creates a SQL command object from the current connection and sets the transaction if one is present.
        /// </summary>
        /// <returns></returns>
        public NpgsqlCommand CreateCommand()
        {
            if(NpgsqlConnection == null)
            {
                throw new DotNetWorkQueueException("An attempt was made to create a SQL command object, but the SQL connection is null");
            }
            var npgsqlCommand = NpgsqlConnection.CreateCommand();
            if (NpgsqlTransaction != null)
            {
                npgsqlCommand.Transaction = NpgsqlTransaction;
            }
            return npgsqlCommand;
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

            if (_npgsqlTransaction != null)
            {
                _npgsqlTransaction.Dispose();
                _npgsqlTransaction = null;
            }

            if (_npgsqlConnection == null) return;

            _npgsqlConnection.Dispose();
            _npgsqlConnection = null;
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
