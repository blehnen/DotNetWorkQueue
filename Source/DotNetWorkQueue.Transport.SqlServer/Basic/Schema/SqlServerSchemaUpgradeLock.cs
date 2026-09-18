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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Schema
{
    /// <summary>
    /// The schema upgrade lock, using SQL Server's application locks.
    /// </summary>
    /// <remarks>
    /// sp_getapplock with an owner of Transaction releases when the transaction ends, however it ends,
    /// so a process dying mid-upgrade cannot leave the lock held.
    /// </remarks>
    public class SqlServerSchemaUpgradeLock : ISchemaUpgradeLock
    {
        /// <inheritdoc />
        public bool TryAcquire(DbConnection connection, DbTransaction transaction, string resourceName,
            TimeSpan timeout)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_getapplock";

                AddParameter(command, "@Resource", DbType.String,
                    SchemaUpgradeLockName.For(resourceName));
                AddParameter(command, "@LockMode", DbType.String, "Exclusive");
                AddParameter(command, "@LockOwner", DbType.String, "Transaction");
                AddParameter(command, "@LockTimeout", DbType.Int32, (int)timeout.TotalMilliseconds);

                var returnValue = command.CreateParameter();
                returnValue.ParameterName = "@Result";
                returnValue.DbType = DbType.Int32;
                returnValue.Direction = ParameterDirection.ReturnValue;
                command.Parameters.Add(returnValue);

                command.ExecuteNonQuery();

                //0 granted, 1 granted after waiting. Negative values are timeout, deadlock, cancellation
                //or a parameter error - all of which mean we do not hold it.
                return returnValue.Value != null && returnValue.Value != DBNull.Value &&
                       Convert.ToInt32(returnValue.Value) >= 0;
            }
        }

        private static void AddParameter(DbCommand command, string name, DbType type, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}
