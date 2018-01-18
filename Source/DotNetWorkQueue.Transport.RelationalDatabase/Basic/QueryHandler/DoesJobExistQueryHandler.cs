// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    public class DoesJobExistQueryHandler<TConnection, TTransaction> : IQueryHandler<DoesJobExistQuery<TConnection, TTransaction>, QueueStatuses>
        where TConnection: class, IDbConnection
        where TTransaction: class, IDbTransaction
    {
        private readonly CommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareQueryHandler<DoesJobExistQuery<TConnection, TTransaction>, QueueStatuses> _prepareQuery;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistQueryHandler{TConnection, TTransaction}" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="readColumn">The read column.</param>
        public DoesJobExistQueryHandler(CommandStringCache commandCache,
            IConnectionInformation connectionInformation,
            IQueryHandler<GetTableExistsQuery, bool> tableExists, 
            TableNameHelper tableNameHelper,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareQueryHandler<DoesJobExistQuery<TConnection, TTransaction>, QueueStatuses> prepareQuery,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _tableExists = tableExists;
            _tableNameHelper = tableNameHelper;
            _dbConnectionFactory = dbConnectionFactory;
            _transactionFactory = transactionFactory;
            _prepareQuery = prepareQuery;
            _readColumn = readColumn;
        }

        /// <inheritdoc />
        public QueueStatuses Handle(DoesJobExistQuery<TConnection, TTransaction> query)
        {
            if (query.Connection != null)
            {
                return RunQuery(query, query.Connection, query.Transaction);
            }

            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    return RunQuery(query, connection, trans);
                }
            }

        }

        private QueueStatuses RunQuery(DoesJobExistQuery<TConnection, TTransaction> query, IDbConnection connection, IDbTransaction transaction)
        {
            var returnStatus = QueueStatuses.NotQueued;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                _prepareQuery.Handle(query, command, CommandStringTypes.DoesJobExist);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnStatus =
                            (QueueStatuses) _readColumn.ReadAsInt32(CommandStringTypes.DoesJobExist, 0, reader);
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
                        if (!reader.Read()) return returnStatus;
                        var scheduleTime =
                            _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastScheduleTime, 0, reader);
                        if (scheduleTime == query.ScheduledTime)
                        {
                            return QueueStatuses.Processed;
                        }
                    }
                }
            }
            return returnStatus;
        }
    }
}
