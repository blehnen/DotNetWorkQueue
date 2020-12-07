// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Collections.Concurrent;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand>
    {
        private readonly IGetTimeFactory _getUtcDateQuery;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly ConcurrentDictionary<string, string> _rollbackDictionary;
        private readonly object _setupSqlLocker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="getUtcDateQuery">The get UTC date query.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public RollbackMessageCommandHandler(IGetTimeFactory getUtcDateQuery,
            IPostgreSqlMessageQueueTransportOptionsFactory options, 
            ITableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            PostgreSqlCommandStringCache commandCache)
        {
            _getUtcDateQuery = getUtcDateQuery;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _rollbackDictionary = new ConcurrentDictionary<string, string>();
        }
        /// <inheritdoc />
        public void Handle(RollbackMessageCommand rollBackCommand)
        {
            SetupSql();
            using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = trans;
                        command.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                        command.Parameters["@QueueID"].Value = rollBackCommand.QueueId;

                        if (_options.Value.EnableDelayedProcessing && rollBackCommand.IncreaseQueueDelay.HasValue)
                        {
                            if (rollBackCommand.LastHeartBeat.HasValue)
                            {
                                command.CommandText = GetRollbackSql(false, true);
                                command.Parameters.Add("@HeartBeat", NpgsqlDbType.Bigint);
                                command.Parameters["@HeartBeat"].Value = rollBackCommand.LastHeartBeat.Value.Ticks;
                            }
                            else
                            {
                                command.CommandText = GetRollbackSql(true, false);
                            }

                            var dtUtcDate = _getUtcDateQuery.Create().GetCurrentUtcDate();
                            dtUtcDate = dtUtcDate.Add(rollBackCommand.IncreaseQueueDelay.Value);
                            command.Parameters.Add("@QueueProcessTime", NpgsqlDbType.Bigint);
                            command.Parameters["@QueueProcessTime"].Value = dtUtcDate.Ticks;
                        }
                        else
                        {
                            if (rollBackCommand.LastHeartBeat.HasValue)
                            {
                                command.CommandText = GetRollbackSql(false, true);
                                command.Parameters.Add("@HeartBeat", NpgsqlDbType.Bigint);
                                command.Parameters["@HeartBeat"].Value = rollBackCommand.LastHeartBeat.Value.Ticks;
                            }
                            else
                            {
                                command.CommandText = GetRollbackSql(false, false);
                            }
                        }
                        command.ExecuteNonQuery();
                    }

                    if (_options.Value.EnableStatusTable)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = trans;
                            command.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                            command.Parameters["@QueueID"].Value = rollBackCommand.QueueId;
                            command.Parameters.Add("@status", NpgsqlDbType.Integer);
                            command.Parameters["@status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
                            command.CommandText =
                                _commandCache.GetCommand(CommandStringTypes.UpdateStatusRecord);
                            command.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
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
