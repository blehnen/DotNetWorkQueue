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
using System.Data.SQLite;
using System.Linq;
using System.Text;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Moves a record from the meta table to the error table
    /// </summary>
    internal class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetColumnNamesFromTableQuery, List<string>> _columnQuery;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IGetTime _getTime;
        private readonly ISqLiteTransactionFactory _transactionFactory;
        private readonly object _buildSqlLocker = new object();

        private string _moveRecordSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandHandler" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="columnQuery">The column query.</param>
        /// <param name="options">The options.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public MoveRecordToErrorQueueCommandHandler(IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper, 
            IQueryHandler<GetColumnNamesFromTableQuery, List<string>> columnQuery,
            ISqLiteMessageQueueTransportOptionsFactory options,
            SqLiteCommandStringCache commandCache,
            IGetTimeFactory getTimeFactory,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => columnQuery, columnQuery);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => commandCache, commandCache);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _columnQuery = columnQuery;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _commandCache = commandCache;
            _getTime = getTimeFactory.Create();
            _transactionFactory = transactionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }

            GenerateSqlForMove();

            using (var conn = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var trans = _transactionFactory.Create(conn).BeginTransaction())
                {
                    using (var commandSql = conn.CreateCommand())
                    {
                        commandSql.CommandText = _moveRecordSql;
                        commandSql.Transaction = trans;
                        commandSql.Parameters.Add("@QueueID", DbType.Int64);
                        commandSql.Parameters.Add("@LastException", DbType.String, -1);
                        commandSql.Parameters.Add("@CurrentDateTime", DbType.Int64);
                        commandSql.Parameters["@QueueID"].Value = command.QueueId;
                        commandSql.Parameters["@LastException"].Value = command.Exception.ToString();
                        commandSql.Parameters["@CurrentDateTime"].Value = _getTime.GetCurrentUtcDate().Ticks;
                        var iCount = commandSql.ExecuteNonQuery();
                        if (iCount != 1) return;

                        //the record is now in the error queue, remove it from the main queue
                        using (
                            var commandSqlDeleteMetaData = conn.CreateCommand())
                        {
                            commandSqlDeleteMetaData.CommandText =
                                _commandCache.GetCommand(SqLiteCommandStringTypes.DeleteFromMetaData);
                            commandSqlDeleteMetaData.Transaction = trans;
                            commandSqlDeleteMetaData.Parameters.Add("@QueueID", DbType.Int64);
                            commandSqlDeleteMetaData.Parameters["@QueueID"].Value = command.QueueId;
                            commandSqlDeleteMetaData.ExecuteNonQuery();
                        }

                        if (!_options.Value.EnableStatusTable)
                        {
                            trans.Commit();
                            return;
                        }

                        //update the status record
                        using (
                            var commandSqlUpdateStatusRecord = conn.CreateCommand())
                        {
                            commandSqlUpdateStatusRecord.CommandText =
                                _commandCache.GetCommand(SqLiteCommandStringTypes.UpdateStatusRecord);
                            commandSqlUpdateStatusRecord.Transaction = trans;
                            commandSqlUpdateStatusRecord.Parameters.Add("@QueueID", DbType.Int64);
                            commandSqlUpdateStatusRecord.Parameters.Add("@Status", DbType.Int32);
                            commandSqlUpdateStatusRecord.Parameters["@QueueID"].Value = command.QueueId;
                            commandSqlUpdateStatusRecord.Parameters["@Status"].Value =
                                Convert.ToInt16(QueueStatuses.Error);
                            commandSqlUpdateStatusRecord.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        /// <summary>
        /// Generates the SQL for moving a record to the error queue.
        /// </summary>
        private void GenerateSqlForMove()
        {
            if (!string.IsNullOrEmpty(_moveRecordSql)) return;
            lock (_buildSqlLocker)
            {
                if (string.IsNullOrEmpty(_moveRecordSql))
                {
                    _moveRecordSql = BuildMoveRecordToErrorQueueSql();
                }
            }
        }

        /// <summary>
        /// Builds the move record to error queue SQL.
        /// </summary>
        /// <returns></returns>
        private string BuildMoveRecordToErrorQueueSql()
        {
            var columnsToCopy =
                GetColumnsThatAreInBothTables(_connectionInformation.ConnectionString,
                    _tableNameHelper.MetaDataErrorsName, _tableNameHelper.MetaDataName).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Insert into " + _tableNameHelper.MetaDataErrorsName);
            sb.Append(" ( ");
            var i = 0;
            foreach (var column in columnsToCopy)
            {
                sb.Append(column);
                if (i < columnsToCopy.Count - 1)
                {
                    sb.Append(",");
                }
                i++;
            }
            sb.Append(", LastException, LastExceptionDate)");

            sb.Append(" select ");
            i = 0;
            foreach (var column in columnsToCopy)
            {
                sb.Append(column);
                if (i < columnsToCopy.Count - 1)
                {
                    sb.Append(",");
                }
                i++;
            }
            sb.Append(", @LastException, @CurrentDateTime from " + _tableNameHelper.MetaDataName);
            sb.Append(" where queueid = @queueid");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the columns that are in both tables.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName1">The table name 1.</param>
        /// <param name="tableName2">The table name 2.</param>
        /// <returns></returns>
        private IEnumerable<string> GetColumnsThatAreInBothTables(string connectionString, string tableName1, string tableName2)
        {
            var columns1 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(connectionString, tableName1));
            var columns2 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(connectionString, tableName2));
            return columns1.Intersect(columns2, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
