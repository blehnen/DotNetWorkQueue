// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using DotNetWorkQueue.Transport.SQLite;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Creates new db objects
    /// </summary>
    /// <remarks>
    /// <para>
    /// Connections handed out for file databases are pooled as <em>managed objects</em> and kept
    /// open between operations. Constructing a <see cref="SQLiteConnection"/> costs roughly 400 us
    /// — far more than opening the database, which costs about 63 us — so reuse of the object is
    /// what matters, and it only pays while the connection stays open. Measured for one insert:
    /// a new connection per operation 695.8 us, an object pool closing on return 188.3 us, an
    /// object pool leaving the connection open 14.8 us.
    /// </para>
    /// <para>
    /// The pool lives here because this class is registered as a container singleton, which ties
    /// pooled connections to the lifetime of the queue that created them. <b>Disposing the
    /// producer or consumer is what releases the database file</b>; a caller that deletes the
    /// database while a queue is still alive will find it locked, as it would have before.
    /// </para>
    /// </remarks>
    /// <seealso cref="DotNetWorkQueue.Transport.SQLite.IDbFactory" />
    public class DbFactory : IDbFactory, IDisposable
    {
        /// <summary>
        /// Bounded so a caller that generates connection strings dynamically cannot accumulate
        /// open connections. Past the cap a returned connection is disposed rather than kept.
        /// </summary>
        private const int MaxPooledPerConnectionString = 32;

        private readonly IContainer _container;
        private readonly IGetFileNameFromConnectionString _getFileName = new GetFileNameFromConnectionString();

        private readonly ConcurrentDictionary<string, ConcurrentBag<SQLiteConnection>> _pool =
            new ConcurrentDictionary<string, ConcurrentBag<SQLiteConnection>>(StringComparer.Ordinal);

        private int _disposeCount;

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
            var applied = ConnectionStringPooling.Apply(connectionString, forMemoryHold);

            //A hold connection is never released, so pooling it would serve no purpose. An
            //in-memory database is kept alive by SqLiteHoldConnection, and holding extra open
            //connections to a shared-cache in-memory database would keep it alive past the point
            //the caller disposed of it.
            if (forMemoryHold || _getFileName.GetFileName(connectionString).IsInMemory)
                return new SQLiteConnection(applied);

            ThrowIfDisposed();
            return new PooledConnection(this, applied, Rent(applied));
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

        private SQLiteConnection Rent(string connectionString)
        {
            if (_pool.TryGetValue(connectionString, out var bag) && bag.TryTake(out var pooled))
            {
                //A connection can go stale while it sits in the pool. Verify before handing it
                //out, so a broken one costs one construction rather than a failed operation.
                if (pooled.State == ConnectionState.Open)
                    return pooled;

                Safely(pooled.Dispose);
            }

            var connection = new SQLiteConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
            return connection;
        }

        /// <summary>
        /// Takes a connection back from a <see cref="PooledConnection"/>. A connection that is not
        /// open, or that arrives after this factory has been disposed, is disposed rather than
        /// kept, so a broken connection cannot poison the next caller.
        /// </summary>
        internal void Return(string connectionString, SQLiteConnection connection)
        {
            //A connection carrying an open transaction would hand that transaction to the next
            //renter. AutoCommit is false exactly while a transaction is open, so this catches a
            //caller that disposed the connection before the transaction.
            if (connection.State != ConnectionState.Open || !connection.AutoCommit)
            {
                Safely(connection.Dispose);
                return;
            }

            var bag = _pool.GetOrAdd(connectionString, _ => new ConcurrentBag<SQLiteConnection>());

            //Count is a snapshot, so this can overshoot by roughly the number of threads returning
            //at once. That is deliberate: the cap exists to bound the pool, not to hold an exact
            //size, and a lock here would sit on every operation.
            if (bag.Count >= MaxPooledPerConnectionString)
            {
                Safely(connection.Dispose);
                return;
            }

            bag.Add(connection);

            //Add first, then re-check disposal. Dispose may have drained the pool between the
            //checks above and this add; without this a connection returned during disposal would
            //survive it and hold the database file open.
            if (IsDisposed)
                DrainAll();
        }

        private bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        private void ThrowIfDisposed()
        {
            //ObjectDisposedException.ThrowIf is .NET 7+
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        private void DrainAll()
        {
            foreach (var bag in _pool.Values)
            {
                while (bag.TryTake(out var connection))
                    Safely(connection.Dispose);
            }
        }

        /// <summary>
        /// Closes every pooled connection. This is what releases the database file, so callers
        /// that delete a SQLite database must dispose the queue first.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the pooled connections.
        /// </summary>
        /// <param name="disposing">true when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            //the flag is raised even on the finalizer path so a concurrent Return stops pooling
            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            if (disposing)
                DrainAll();
        }

        /// <summary>
        /// Disposal of a connection that is already broken can itself throw, and there is nothing
        /// useful to do about it — the connection is being discarded either way.
        /// </summary>
        private static void Safely(Action action)
        {
            try
            {
                action();
            }
            catch (SQLiteException)
            {
                //discarding a broken connection
            }
            catch (InvalidOperationException)
            {
                //discarding a broken connection
            }
        }
    }
}
