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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using System;
using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand<long>>,
        ICommandHandlerAsync<RollbackMessageCommand<long>>
    {
        private readonly IGetTimeFactory _getUtcDateQuery;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly ConcurrentDictionary<string, string> _rollbackDictionary;
        private readonly object _setupSqlLocker = new object();
        private const string QueueIdParameter = "@QueueID";
        private const string HeartBeatParameter = "@HeartBeat";

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="getUtcDateQuery">The get UTC date query.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public RollbackMessageCommandHandler(IGetTimeFactory getUtcDateQuery,
            ISqlServerMessageQueueTransportOptionsFactory options,
            ITableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            SqlServerCommandStringCache commandCache)
        {
            _getUtcDateQuery = getUtcDateQuery;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _rollbackDictionary = new ConcurrentDictionary<string, string>();
        }
        /// <summary>
        /// Handles the specified rollback command.
        /// </summary>
        /// <param name="rollBackCommand">The rollBackCommand.</param>
        public void Handle(RollbackMessageCommand<long> rollBackCommand)
        {
            SetupSql();
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        if (PrepareRollback(command, trans, rollBackCommand))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    if (_options.Value.EnableStatusTable)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            PrepareStatus(command, trans, rollBackCommand);
                            command.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        /// <inheritdoc />
        public async Task HandleAsync(RollbackMessageCommand<long> rollBackCommand)
        {
            SetupSql();
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var trans = (SqlTransaction)await connection.BeginTransactionAsync().ConfigureAwait(false))
                {
                    using (var command = connection.CreateCommand())
                    {
                        if (PrepareRollback(command, trans, rollBackCommand))
                        {
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }

                    if (_options.Value.EnableStatusTable)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            PrepareStatus(command, trans, rollBackCommand);
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                    await trans.CommitAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sets the transaction, parameters and text for the rollback statement.
        /// </summary>
        /// <returns>false when there is no statement to run.</returns>
        /// <remarks>
        /// Shared by both members rather than duplicated into the asynchronous one. Which statement
        /// this picks depends on the delayed-processing option and whether a heartbeat was recorded,
        /// and two copies of that choice would be free to drift - which is how the missing
        /// metadata-errors delete in #291 and the untagged commit span in #295 came about.
        /// </remarks>
        private bool PrepareRollback(SqlCommand command, SqlTransaction trans, RollbackMessageCommand<long> rollBackCommand)
        {
            command.Transaction = trans;
            command.Parameters.Add(QueueIdParameter, SqlDbType.BigInt);
            command.Parameters[QueueIdParameter].Value = rollBackCommand.QueueId;

            if (_options.Value.EnableDelayedProcessing && rollBackCommand.IncreaseQueueDelay.HasValue)
            {
                if (rollBackCommand.LastHeartBeat.HasValue)
                {
                    command.CommandText = GetRollbackSql(false, true);
                    command.Parameters.Add(HeartBeatParameter, SqlDbType.DateTime);
                    command.Parameters[HeartBeatParameter].Value = rollBackCommand.LastHeartBeat.Value;
                }
                else
                {
                    command.CommandText = GetRollbackSql(true, false);
                }

                var dtUtcDate = _getUtcDateQuery.Create().GetCurrentUtcDate();
                dtUtcDate = dtUtcDate.Add(rollBackCommand.IncreaseQueueDelay.Value);
                command.Parameters.Add("@QueueProcessTime", SqlDbType.DateTime);
                command.Parameters["@QueueProcessTime"].Value = dtUtcDate;
            }
            else
            {
                if (rollBackCommand.LastHeartBeat.HasValue)
                {
                    command.CommandText = GetRollbackSql(false, true);
                    command.Parameters.Add(HeartBeatParameter, SqlDbType.DateTime);
                    command.Parameters[HeartBeatParameter].Value = rollBackCommand.LastHeartBeat.Value;
                }
                else
                {
                    command.CommandText = GetRollbackSql(false, false);
                }
            }

            return !string.IsNullOrEmpty(command.CommandText);
        }

        /// <summary>
        /// Sets the transaction, parameters and text for the status-table update.
        /// </summary>
        private void PrepareStatus(SqlCommand command, SqlTransaction trans, RollbackMessageCommand<long> rollBackCommand)
        {
            command.Transaction = trans;
            command.Parameters.Add(QueueIdParameter, SqlDbType.BigInt);
            command.Parameters[QueueIdParameter].Value = rollBackCommand.QueueId;
            command.Parameters.Add("@status", SqlDbType.Int);
            command.Parameters["@status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
            command.CommandText = _commandCache.GetCommand(CommandStringTypes.UpdateStatusRecord);
        }

        /// <summary>
        /// Setups the SQL for rollbacks
        /// </summary>
        private void SetupSql()
        {
            if (_rollbackDictionary.Count == 4) return;
            lock (_setupSqlLocker)
            {
                if (_rollbackDictionary.Count == 4) return;
                _rollbackDictionary.TryAdd("TrueTrue", GetRollbackSql(true, true));
                _rollbackDictionary.TryAdd("TrueFalse", GetRollbackSql(true, false));
                _rollbackDictionary.TryAdd("FalseFalse", GetRollbackSql(false, false));
                _rollbackDictionary.TryAdd("FalseTrue", GetRollbackSql(false, true));
            }
        }

        /// <summary>
        /// Gets the rollback SQL.
        /// </summary>
        /// <param name="includeDateIfEnabled">if set to <c>true</c> [include date if enabled].</param>
        /// <param name="includeHeartBeatDate">if set to <c>true</c> [include heart beat date].</param>
        /// <returns></returns>
        private string GetRollbackSql(bool includeDateIfEnabled, bool includeHeartBeatDate)
        {
            var key = string.Concat(includeDateIfEnabled.ToString(), includeHeartBeatDate.ToString());
            // ReSharper disable once InconsistentlySynchronizedField
            return _rollbackDictionary.Count == 4 ? _rollbackDictionary[key] : GetRollbackSqlGen(includeDateIfEnabled, includeHeartBeatDate);
        }

        /// <summary>
        /// Generates the SQL statement for rolling back a unit of work
        /// </summary>
        /// <param name="includeDateIfEnabled">if set to <c>true</c> [include date if enabled].</param>
        /// <param name="includeHeartBeatDate">if set to <c>true</c> [include heart beat date].</param>
        /// <returns></returns>
        private string GetRollbackSqlGen(bool includeDateIfEnabled, bool includeHeartBeatDate)
        {
            var sb = new StringBuilder();
            sb.Append("Update " + _tableNameHelper.MetaDataName + " set ");
            var bNeedComma = false;
            if (_options.Value.EnableDelayedProcessing && includeDateIfEnabled)
            {
                sb.Append(" QueueProcessTime = @QueueProcessTime ");
                bNeedComma = true;
            }
            if (_options.Value.EnableHeartBeat)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" HeartBeat = null ");
                bNeedComma = true;
            }
            if (_options.Value.EnableStatus)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendFormat(" status = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                bNeedComma = true;
            }
            if (!bNeedComma)
            {
                //no columns to update - nothing to rollback
                return string.Empty;
            }
            sb.Append(" where queueid = @queueid");
            if (includeHeartBeatDate)
            {
                sb.Append(" AND heartbeat = @HeartBeat");
            }
            return sb.ToString();
        }
    }
}
