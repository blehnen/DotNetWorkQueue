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
using System.Data;
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Per-message state carrying the active dequeue <see cref="IDbConnection"/> and
    /// <see cref="IDbTransaction"/> when
    /// <c>SqLiteMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted</c>
    /// is true. Stored on <see cref="IMessageContext"/> via
    /// <see cref="SqLiteHeaders.ConnectionState"/>; read by the receive-path commit /
    /// rollback / cleanup delegates and by the inbox <c>IRelationalWorkerNotification</c>
    /// implementation.
    /// </summary>
    /// <remarks>
    /// Phase 5 introduced this type to add hold-transaction semantics to the SQLite
    /// transport for the first time. SqlServer and PostgreSQL achieve the same outcome
    /// via the typed <see cref="DotNetWorkQueue.Transport.RelationalDatabase.IConnectionHolder{TConnection,TTransaction,TCommand}"/>
    /// abstraction; SQLite uses this lighter-weight context-state pattern because the
    /// existing SQLite receive pipeline operates on the <see cref="IDbConnection"/> /
    /// <see cref="IDbTransaction"/> interfaces throughout, so introducing a typed holder
    /// would have been an unnecessary parallel abstraction.
    /// </remarks>
    internal sealed class SqLiteConnectionState
    {
        /// <summary>
        /// Gets the active dequeue connection. Lifecycle is owned by the receive path;
        /// disposed in <c>Context_Cleanup</c>.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Gets the active dequeue transaction. Lifecycle is owned by the receive path;
        /// committed by <c>ContextOnCommit</c> on successful handler return, rolled back
        /// by <c>ContextOnRollback</c> on handler throw, disposed in <c>Context_Cleanup</c>.
        /// </summary>
        public IDbTransaction Transaction { get; }

        private int _completed;

        /// <summary>
        /// Gets a value indicating whether the transaction has been committed or rolled
        /// back. Used to guard against double-commit / double-rollback in the cleanup
        /// path. Backed by an <see cref="Interlocked"/>-managed flag so the commit and
        /// cleanup paths cannot race on a non-atomic <c>bool</c>.
        /// </summary>
        public bool Completed => Volatile.Read(ref _completed) != 0;

        /// <summary>
        /// Initializes a new instance of <see cref="SqLiteConnectionState"/>.
        /// </summary>
        /// <param name="connection">The active dequeue connection. Must be non-null and open.</param>
        /// <param name="transaction">The active dequeue transaction. Must be non-null.</param>
        public SqLiteConnectionState(IDbConnection connection, IDbTransaction transaction)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => transaction, transaction);
            Connection = connection;
            Transaction = transaction;
        }

        /// <summary>
        /// Marks the held transaction as completed (committed or rolled back). Subsequent
        /// commit / rollback attempts in the cleanup path become no-ops. Returns
        /// <see langword="true"/> on the first call and <see langword="false"/> on any
        /// subsequent call, so callers can detect the race and skip a duplicate commit /
        /// rollback.
        /// </summary>
        public bool MarkCompleted() => Interlocked.Exchange(ref _completed, 1) == 0;
    }
}
