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
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class DoesJobExistQueryHandler : IQueryHandler<DoesJobExistQuery, QueueStatus>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRecordExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public DoesJobExistQueryHandler(SqlServerCommandStringCache commandCache,
            IConnectionInformation connectionInformation, IQueryHandler<GetTableExistsQuery, bool> tableExists, 
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _tableExists = tableExists;
            _tableNameHelper = tableNameHelper;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public QueueStatus Handle(DoesJobExistQuery query)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.DoesJobExist);

                    command.Parameters.Add("@JobName", SqlDbType.VarChar, 255);
                    command.Parameters["@JobName"].Value = query.JobName;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (QueueStatus) reader.GetInt32(0);
                        }
                    }
                }
            }
            return QueueStatus.NotQueued;
        }

        private QueueStatus RunQuery(DoesJobExistQuery query, SqlConnection connection, SqlTransaction transaction)
        {
            var returnStatus = QueueStatus.NotQueued;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.DoesJobExist);
                command.Transaction = transaction;
                command.Parameters.Add("@JobName", SqlDbType.VarChar, 255);
                command.Parameters["@JobName"].Value = query.JobName;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnStatus = (QueueStatus)reader.GetInt32(0);
                    }
                }

                if (returnStatus == QueueStatus.NotQueued &&
                    _tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                        _tableNameHelper.JobTableName)))
                {
                    command.CommandText =
                        _commandCache.GetCommand(SqlServerCommandStringTypes.GetJobLastScheduleTime);
                    command.Transaction = transaction;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var data = reader.GetDateTimeOffset(0);
                            if (data == query.ScheduledTime)
                            {
                                return QueueStatus.Processed;
                            }
                        }
                    }
                }
            }
            return returnStatus;
        }
    }
}
