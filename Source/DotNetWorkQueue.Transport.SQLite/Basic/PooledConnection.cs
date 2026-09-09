// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// An <see cref="IDbConnection"/> that hands its underlying connection back to a pool when
    /// disposed, instead of closing it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The expensive part of using a connection here is neither opening the database nor the
    /// provider's own pool — it is constructing the <see cref="SQLiteConnection"/> object.
    /// Measured on net10: <c>new SQLiteConnection()</c> with no arguments costs ~400 us, opening
    /// the database adds ~63 us, and assigning a connection string to an object that already
    /// exists costs 0.007 us. The provider pools the native handle, which is the cheap part, so
    /// <c>Pooling=True</c> alone recovers little.
    /// </para>
    /// <para>
    /// Reusing the managed object is therefore what matters, and it only pays if the connection
    /// stays open: measured for one insert, a new connection per operation cost 695.8 us, an
    /// object pool that closed on return cost 188.3 us, and an object pool that left the
    /// connection open cost 14.8 us.
    /// </para>
    /// <para>
    /// Each rented connection is owned exclusively for one operation, exactly as a freshly
    /// constructed one was, so nothing is shared between threads and no locking is required.
    /// </para>
    /// </remarks>
    internal sealed class PooledConnection : DbConnection
    {
        private readonly DbFactory _owner;
        private readonly string _connectionString;
        private PooledConnectionEntry _entry;
        private int _disposeCount;

        internal PooledConnection(DbFactory owner, string connectionString, PooledConnectionEntry entry)
        {
            _owner = owner;
            _connectionString = connectionString;
            _entry = entry;
        }

        private PooledConnectionEntry Entry =>
            _entry ?? throw new ObjectDisposedException(nameof(PooledConnection));

        private SQLiteConnection Inner => Entry.Connection;

        /// <summary>
        /// A command for <paramref name="commandText"/>, reusing the statements SQLite compiled for
        /// it on this connection. See <see cref="PooledCommand"/> for why that matters.
        /// </summary>
        internal DbCommand CreateCommand(string commandText) => (DbCommand)Entry.CreateCommand(commandText);

        /// <summary>How many distinct commands this connection is holding compiled statements for.</summary>
        internal int CachedCommandCount => Entry.CachedCommandCount;

        /// <summary>
        /// No-op. A pooled connection is handed out already open; callers still call
        /// <c>Open()</c> because that is the contract for a freshly created connection, and
        /// calling it on an open <see cref="SQLiteConnection"/> would throw.
        /// </summary>
        public override void Open()
        {
            if (Inner.State != ConnectionState.Open)
                Inner.Open();
        }

        /// <summary>
        /// No-op. The connection is returned to the pool on <see cref="Dispose"/>, and closing it
        /// here would discard the reuse this type exists to provide.
        /// </summary>
        public override void Close()
        {
            //deliberately does nothing; see Dispose
        }

        /// <inheritdoc />
        /// <remarks>
        /// DbConnection supplies the parameterless BeginTransaction and CreateCommand, which route
        /// through these two, so the interface versions are no longer declared here.
        /// </remarks>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            Inner.BeginTransaction(isolationLevel);

        /// <inheritdoc />
        protected override DbCommand CreateDbCommand() => Inner.CreateCommand();

        /// <inheritdoc />
        public override void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

        /// <inheritdoc />
        public override string ConnectionString
        {
            //the string this connection was rented for; the provider may normalise its own copy
            get => _connectionString;
            set => throw new NotSupportedException(
                "The connection string of a pooled connection cannot be changed; request a new connection instead.");
        }

        /// <inheritdoc />
        public override int ConnectionTimeout => Inner.ConnectionTimeout;

        /// <inheritdoc />
        public override string Database => Inner.Database;

        /// <inheritdoc />
        public override string DataSource => Inner.DataSource;

        /// <inheritdoc />
        public override string ServerVersion => Inner.ServerVersion;

        /// <inheritdoc />
        public override ConnectionState State => _entry?.Connection.State ?? ConnectionState.Closed;

        /// <summary>
        /// Returns the underlying connection to the pool rather than closing it. A connection that
        /// is no longer open — because the operation failed, or the provider dropped it — is
        /// disposed instead, so a broken connection cannot poison the next caller.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                base.Dispose(false);
                return;
            }

            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            var entry = Interlocked.Exchange(ref _entry, null);
            if (entry == null)
                return;

            _owner.Return(_connectionString, entry);
        }
    }
}
