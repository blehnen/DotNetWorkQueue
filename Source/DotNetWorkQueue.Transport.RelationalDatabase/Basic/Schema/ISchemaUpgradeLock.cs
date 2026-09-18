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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Stops two processes upgrading the same queue at once.
    /// </summary>
    /// <remarks>
    /// Needed because a deploy across several nodes will call the upgrade from all of them at the same
    /// moment. Without it, two processes read the same starting version and both apply the same
    /// scripts, which for DDL means the second fails and for data means it runs twice.
    ///
    /// Held for the life of the upgrade transaction and released when that transaction ends, however it
    /// ends. That is deliberate: a lock outliving its transaction is a lock that can be orphaned by a
    /// process dying mid-upgrade, and every transport here has a transaction-scoped primitive that
    /// cannot be.
    /// </remarks>
    public interface ISchemaUpgradeLock
    {
        /// <summary>
        /// Takes the upgrade lock for a queue, inside the supplied transaction.
        /// </summary>
        /// <param name="connection">The connection the upgrade is running on.</param>
        /// <param name="transaction">The transaction the lock is scoped to.</param>
        /// <param name="resourceName">Identifies the queue being upgraded.</param>
        /// <param name="timeout">How long to wait before giving up.</param>
        /// <returns>
        /// True if the lock is held. False if another process holds it - which is not an error: that
        /// process is doing the work, and the caller should re-read the version rather than retry.
        /// </returns>
        bool TryAcquire(DbConnection connection, DbTransaction transaction, string resourceName,
            TimeSpan timeout);
    }
}
