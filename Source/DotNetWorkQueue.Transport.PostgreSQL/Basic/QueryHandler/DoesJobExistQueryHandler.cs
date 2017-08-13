// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class DoesJobExistQueryHandler : IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses>
    {
        private readonly PostgreSqlCommandStringCache _commandCache;
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
        public DoesJobExistQueryHandler(PostgreSqlCommandStringCache commandCache,
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
        public QueueStatuses Handle(DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction> query)
        {
            if (query.Connection != null)
            {
                return RunQuery(query, query.Connection, query.Transaction);
            }
            using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    return RunQuery(query, connection, transaction);
                }
            }
        }

        private QueueStatuses RunQuery(DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction> query, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var returnStatus = QueueStatuses.NotQueued;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.DoesJobExist);
                command.Transaction = transaction;
                command.Parameters.Add("@JobName", NpgsqlDbType.Varchar, 255);
                command.Parameters["@JobName"].Value = query.JobName;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnStatus = (QueueStatuses)reader.GetInt32(0);
                    }
                }

                if (returnStatus == QueueStatuses.NotQueued &&
                    _tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                        _tableNameHelper.JobTableName)))
                {
                    command.CommandText =
                        _commandCache.GetCommand(CommandStringTypes.GetJobLastScheduleTime);
                    command.Transaction = transaction;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var data = new DateTimeOffset(new DateTime(reader.GetInt64(0), DateTimeKind.Utc));
                            if (data == query.ScheduledTime)
                            {
                                return QueueStatuses.Processed;
                            }
                        }
                    }
                }
            }
            return returnStatus;
        }
    }
}
