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
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
 
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Moves a record from the meta table to the error table
    /// </summary>
    internal class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetColumnNamesFromTableQuery, List<string>> _columnQuery;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly SqlHeaders _headers;
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
        /// <param name="headers">The headers.</param>
        public MoveRecordToErrorQueueCommandHandler(IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper, 
            IQueryHandler<GetColumnNamesFromTableQuery, List<string>> columnQuery,
            ISqlServerMessageQueueTransportOptionsFactory options,
            SqlServerCommandStringCache commandCache,
            SqlHeaders headers)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => columnQuery, columnQuery);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => headers, headers);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _columnQuery = columnQuery;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
            _commandCache = commandCache;
            _headers = headers;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            GenerateSqlForMove();
            if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                HandleForTransaction(command);
            }
            else
            {
                using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        using (var commandSql = conn.CreateCommand())
                        {
                            commandSql.CommandText = _moveRecordSql;
                            commandSql.Transaction = trans;
                            commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                            commandSql.Parameters.Add("@LastException", SqlDbType.VarChar, -1);
                            commandSql.Parameters["@QueueID"].Value = command.QueueId;
                            commandSql.Parameters["@LastException"].Value = command.Exception.ToString();
                            var iCount = commandSql.ExecuteNonQuery();
                            if (iCount != 1) return;

                            //the record is now in the error queue, remove it from the main queue
                            using (
                                var commandSqlDeleteMetaData = conn.CreateCommand())
                            {
                                commandSqlDeleteMetaData.CommandText =
                                    _commandCache.GetCommand(CommandStringTypes.DeleteFromMetaData);
                                commandSqlDeleteMetaData.Transaction = trans;
                                commandSqlDeleteMetaData.Parameters.Add("@QueueID", SqlDbType.BigInt);
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
                                    _commandCache.GetCommand(CommandStringTypes.UpdateStatusRecord);
                                commandSqlUpdateStatusRecord.Transaction = trans;
                                commandSqlUpdateStatusRecord.Parameters.Add("@QueueID", SqlDbType.BigInt);
                                commandSqlUpdateStatusRecord.Parameters.Add("@Status", SqlDbType.Int);
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
        }

        private void HandleForTransaction(MoveRecordToErrorQueueCommand command)
        {
            var conn = command.MessageContext.Get(_headers.Connection);
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.CommandText = _moveRecordSql;
                commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                commandSql.Parameters.Add("@LastException", SqlDbType.VarChar, -1);
                commandSql.Parameters["@QueueID"].Value = command.QueueId;
                commandSql.Parameters["@LastException"].Value = command.Exception.ToString();
                var iCount = commandSql.ExecuteNonQuery();
                if (iCount != 1) return;

                //the record is now in the error queue, remove it from the main queue
                using (
                    var commandSqlDeleteMetaData = conn.CreateCommand())
                {
                    commandSqlDeleteMetaData.CommandText =
                        _commandCache.GetCommand(CommandStringTypes.DeleteFromMetaData);
                    commandSqlDeleteMetaData.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    commandSqlDeleteMetaData.Parameters["@QueueID"].Value = command.QueueId;
                    commandSqlDeleteMetaData.ExecuteNonQuery();
                }

                if (_options.Value.EnableStatusTable)
                {
                    //update the status record
                    using (
                        var commandSqlUpdateStatusRecord = conn.CreateCommand())
                    {
                        commandSqlUpdateStatusRecord.CommandText =
                            _commandCache.GetCommand(CommandStringTypes.UpdateStatusRecord);
                        commandSqlUpdateStatusRecord.Parameters.Add("@QueueID", SqlDbType.BigInt);
                        commandSqlUpdateStatusRecord.Parameters.Add("@Status", SqlDbType.Int);
                        commandSqlUpdateStatusRecord.Parameters["@QueueID"].Value = command.QueueId;
                        commandSqlUpdateStatusRecord.Parameters["@Status"].Value =
                            Convert.ToInt16(QueueStatuses.Error);
                        commandSqlUpdateStatusRecord.ExecuteNonQuery();
                    }
                }
            }

            //at this point, we commit the transaction since we just moved the record
            conn.SqlTransaction.Commit();
            conn.SqlTransaction = null;
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
            sb.Append(", @LastException, GetUTCDate() from " + _tableNameHelper.MetaDataName);
            if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                sb.Append(" WITH (NOLOCK)"); //perform a dirty read on our own transaction so that we can access the record that we've already deleted
            }
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
