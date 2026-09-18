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
using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Schema
{
    /// <summary>
    /// The schema upgrade lock, using PostgreSQL's transaction-scoped advisory locks.
    /// </summary>
    /// <remarks>
    /// pg_advisory_xact_lock releases when the transaction ends, however it ends, so a process dying
    /// mid-upgrade cannot leave the lock held.
    ///
    /// The try-and-wait form is used rather than the blocking one. Blocking would need
    /// <c>SET LOCAL lock_timeout</c> and then catching a specific SQLSTATE to tell "someone else has
    /// it" from a real fault, and a failed try is just a false here.
    /// </remarks>
    public class PostgreSqlSchemaUpgradeLock : ISchemaUpgradeLock
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        /// <inheritdoc />
        public bool TryAcquire(DbConnection connection, DbTransaction transaction, string resourceName,
            TimeSpan timeout)
        {
            var key = SchemaUpgradeLockName.KeyFor(resourceName);
            var deadline = DateTime.UtcNow.Add(timeout);

            while (true)
            {
                if (TryOnce(connection, transaction, key))
                    return true;

                if (DateTime.UtcNow >= deadline)
                    return false;

                Thread.Sleep(PollInterval);
            }
        }

        private static bool TryOnce(DbConnection connection, DbTransaction transaction, long key)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "select pg_try_advisory_xact_lock(@Key)";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@Key";
                parameter.DbType = DbType.Int64;
                parameter.Value = key;
                command.Parameters.Add(parameter);

                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value && Convert.ToBoolean(result);
            }
        }
    }
}
