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
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using System.Text;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand>
    {
        private readonly IGetTimeFactory _getUtcDateQuery;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly ConcurrentDictionary<string, string> _rollbackDictionary;
        private readonly ISqLiteTransactionFactory _transactionFactory;
        private readonly object _setupSqlLocker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="getUtcDateQuery">The get UTC date query.</param>
        /// <param name="options">The options.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public RollbackMessageCommandHandler(IGetTimeFactory getUtcDateQuery,
            ISqLiteMessageQueueTransportOptionsFactory options, 
            TableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            SqLiteCommandStringCache commandCache,
            ISqLiteTransactionFactory transactionFactory)
        {
            _getUtcDateQuery = getUtcDateQuery;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _rollbackDictionary = new ConcurrentDictionary<string, string>();
            _transactionFactory = transactionFactory;
        }
        /// <summary>
        /// Handles the specified rollback command.
        /// </summary>
        /// <param name="rollbackcommand">The rollbackcommand.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public void Handle(RollbackMessageCommand rollbackcommand)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }

            SetupSql();
            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = trans;
                        command.Parameters.Add("@QueueID", DbType.Int64);
                        command.Parameters["@QueueID"].Value = rollbackcommand.QueueId;

                        if (_options.Value.EnableDelayedProcessing && rollbackcommand.IncreaseQueueDelay.HasValue)
                        {
                            if (rollbackcommand.LastHeartBeat.HasValue)
                            {
                                command.CommandText = GetRollbackSql(false, true);
                                command.Parameters.Add("@HeartBeat", DbType.DateTime2);
                                command.Parameters["@HeartBeat"].Value = rollbackcommand.LastHeartBeat.Value.Ticks;
                            }
                            else
                            {
                                command.CommandText = GetRollbackSql(true, false);
                            }

                            var dtUtcDate = _getUtcDateQuery.Create().GetCurrentUtcDate();
                            var dtUtcDateIncreased = dtUtcDate.Add(rollbackcommand.IncreaseQueueDelay.Value);
                            command.Parameters.Add("@QueueProcessTime", DbType.DateTime2);
                            command.Parameters["@QueueProcessTime"].Value = dtUtcDateIncreased.Ticks;
                        }
                        else
                        {
                            if (rollbackcommand.LastHeartBeat.HasValue)
                            {
                                command.CommandText = GetRollbackSql(false, true);
                                command.Parameters.Add("@HeartBeat", DbType.DateTime2);
                                command.Parameters["@HeartBeat"].Value = rollbackcommand.LastHeartBeat.Value.Ticks;
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
                            command.Parameters.Add("@QueueID", DbType.Int64);
                            command.Parameters["@QueueID"].Value = rollbackcommand.QueueId;
                            command.Parameters.Add("@status", DbType.Int32);
                            command.Parameters["@status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
                            command.CommandText =
                                _commandCache.GetCommand(SqLiteCommandStringTypes.UpdateStatusRecord);
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
