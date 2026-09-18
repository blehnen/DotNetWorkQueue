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
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Schema
{
    /// <summary>
    /// The schema upgrade lock for SQLite, which is the database's own write lock.
    /// </summary>
    /// <remarks>
    /// SQLite has no advisory lock to take, and it does not need one: it allows a single writer, so two
    /// processes cannot both commit an upgrade. The second either blocks until the first finishes or
    /// fails busy, and in both cases its transaction rolls back whole. Nothing is applied twice and
    /// nothing is left half applied, which is what the lock exists to guarantee.
    ///
    /// The difference from the other transports is in how losing looks, not in whether it is safe.
    /// Elsewhere the loser waits and then finds the work already done, and reports AlreadyCurrent; here
    /// the loser can surface a busy error and report Failed, having changed nothing. Running the
    /// upgrade again then reports AlreadyCurrent, because the winner's work is committed.
    ///
    /// Returning true rather than pretending to take something is deliberate: a lock implementation
    /// that appears to acquire a real lock here would be a claim this transport cannot back.
    /// </remarks>
    public class SqLiteSchemaUpgradeLock : ISchemaUpgradeLock
    {
        /// <inheritdoc />
        public bool TryAcquire(DbConnection connection, DbTransaction transaction, string resourceName,
            TimeSpan timeout)
        {
            return true;
        }
    }
}
