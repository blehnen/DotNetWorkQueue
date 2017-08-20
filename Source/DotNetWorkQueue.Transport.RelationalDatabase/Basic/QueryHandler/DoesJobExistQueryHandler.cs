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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// 
    /// </summary>
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
        private readonly IDateTimeOffsetParser _dateTimeOffsetParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoesJobExistQueryHandler{TConnection, TTransaction}" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableExists">The table exists.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="dateTimeOffsetParser">The date time offset parser.</param>
        public DoesJobExistQueryHandler(CommandStringCache commandCache,
            IConnectionInformation connectionInformation,
            IQueryHandler<GetTableExistsQuery, bool> tableExists, 
            TableNameHelper tableNameHelper,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IDateTimeOffsetParser dateTimeOffsetParser)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => dateTimeOffsetParser, dateTimeOffsetParser);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _tableExists = tableExists;
            _tableNameHelper = tableNameHelper;
            _dbConnectionFactory = dbConnectionFactory;
            _transactionFactory = transactionFactory;
            _dateTimeOffsetParser = dateTimeOffsetParser;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
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
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.DoesJobExist);
                command.Transaction = transaction;

                var param = command.CreateParameter();
                param.ParameterName = "@JobName";
                param.Size = 255;
                param.DbType = DbType.AnsiString;
                param.Value = query.JobName;
                command.Parameters.Add(param);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnStatus = (QueueStatuses) reader.GetInt32(0);
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
                        var scheduleTime = _dateTimeOffsetParser.Parse(reader[0]);
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
