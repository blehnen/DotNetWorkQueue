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
using System.Data.Common;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Extends <see cref="IWorkerNotification"/> on the three relational transports (SqlServer, PostgreSQL, SQLite)
    /// to expose the active dequeue <see cref="DbTransaction"/> to the message handler, enabling the
    /// transactional inbox pattern: business writes can join the same transaction the library uses to dequeue
    /// and commit the queue message, so the two commit (or roll back) atomically.
    /// </summary>
    /// <remarks>
    /// Capability-cast pattern. Non-relational transports (Memory, Redis, LiteDb) never implement this interface.
    /// User handlers discover the capability via a single cast:
    /// <code>
    /// if (notification is IRelationalWorkerNotification relational)
    /// {
    ///     // write business data on relational.Transaction.Connection within relational.Transaction
    /// }
    /// </code>
    /// The interface is only implemented when <c>EnableHoldTransactionUntilMessageCommitted = true</c> on the
    /// transport options. With the option off, the cast cleanly fails and the inbox capability is not exposed.
    /// <para>
    /// Ownership contract — the library owns the transaction. User handlers MUST NOT call
    /// <c>Commit()</c>, <c>Rollback()</c>, <c>Dispose()</c>, or <c>Close()</c> on the exposed
    /// <see cref="DbTransaction"/> or its <see cref="DbTransaction.Connection"/>. User handlers MUST NOT stash
    /// the reference past the handler's return and MUST NOT pass it to another thread
    /// (<see cref="DbTransaction"/> is not thread-safe). The library commits on successful handler return and
    /// rolls back on handler throw — user signals rollback by throwing.
    /// </para>
    /// </remarks>
    public interface IRelationalWorkerNotification : IWorkerNotification
    {
        /// <summary>
        /// Gets the active <see cref="DbTransaction"/> for the in-flight dequeue. The user's handler may
        /// enlist business writes against this transaction's <see cref="DbTransaction.Connection"/>.
        /// </summary>
        /// <value>
        /// A non-null <see cref="DbTransaction"/> owned by the library. The transaction is committed by the
        /// library after the handler returns successfully and rolled back if the handler throws. The user
        /// must not mutate its lifecycle.
        /// </value>
        /// <remarks>
        /// Typed as the abstract <see cref="DbTransaction"/> (from <c>System.Data.Common</c>), not the
        /// <see cref="System.Data.IDbTransaction"/> interface, so callers may await async dispose / commit
        /// shapes the abstract base exposes. Never null when the containing interface is implemented.
        /// </remarks>
        DbTransaction Transaction { get; }
    }
}
