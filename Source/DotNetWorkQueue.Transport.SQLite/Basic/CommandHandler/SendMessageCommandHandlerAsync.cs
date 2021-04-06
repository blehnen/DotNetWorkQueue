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
using System.Data;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommand, long>
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IDbCommandStringCache _commandCache;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly IGetTime _getTime;
        private readonly IDbFactory _dbFactory;
        private readonly ICommandHandler<SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>> _sendJobStatus;
        private readonly IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> _jobExistsHandler;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly DatabaseExists _databaseExists;
        private readonly IReaderAsync _readerAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configurationSend">The configuration send.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="dbFactory">The database factory.</param>
        /// <param name="sendJobStatus">The send job status.</param>
        /// <param name="jobExistsHandler">The job exists handler.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        /// <param name="databaseExists">The database exists.</param>
        /// <param name="readerAsync">The reader asynchronous.</param>
        public SendMessageCommandHandlerAsync(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            IDbCommandStringCache commandCache, 
            TransportConfigurationSend configurationSend,
            IGetTimeFactory getTimeFactory,
            IDbFactory dbFactory,
            ICommandHandler<SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>> sendJobStatus, IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> jobExistsHandler, 
            IJobSchedulerMetaData jobSchedulerMetaData,
            DatabaseExists databaseExists,
            IReaderAsync readerAsync)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            Guard.NotNull(() => sendJobStatus, sendJobStatus);
            Guard.NotNull(() => jobExistsHandler, jobExistsHandler);
            Guard.NotNull(() => jobSchedulerMetaData, jobSchedulerMetaData);
            Guard.NotNull(() => databaseExists, databaseExists);
            Guard.NotNull(() => readerAsync, readerAsync);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _commandCache = commandCache;
            _configurationSend = configurationSend;
            _getTime = getTimeFactory.Create();
            _dbFactory = dbFactory;
            _sendJobStatus = sendJobStatus;
            _jobExistsHandler = jobExistsHandler;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _databaseExists = databaseExists;
            _readerAsync = readerAsync;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="commandSend">The command.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to insert record - the ID of the new record returned by SQLite was 0</exception>
        public async Task<long> HandleAsync(SendMessageCommand commandSend)
        {
            if (!_databaseExists.Exists(_configurationSend.ConnectionInfo.ConnectionString))
            {
                return 0;
            }

            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
            }

            using (var connection = _dbFactory.CreateConnection(_configurationSend.ConnectionInfo.ConnectionString, false))
            {
                connection.Open();

                var expiration = TimeSpan.Zero;
                if (_messageExpirationEnabled.Value)
                {
                    expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
                }

                var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
                var scheduledTime = DateTimeOffset.MinValue;
                var eventTime = DateTimeOffset.MinValue;
                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                    eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
                }

                IDbCommand commandStatus = null;
                using (var command = SendMessage.GetMainCommand(commandSend, connection, _commandCache, _headers, _serializer))
                {
                    long id;
                    using (var commandMeta = SendMessage.CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration,
                         connection, commandSend.MessageToSend, commandSend.MessageData, _tableNameHelper, _headers,
                         _options.Value, _getTime))
                    {
                        if (_options.Value.EnableStatusTable)
                        {
                            commandStatus = CreateStatusRecord(connection, commandSend.MessageToSend,
                                commandSend.MessageData);
                        }

                        using (var trans = _dbFactory.CreateTransaction(connection).BeginTransaction())
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(jobName) || 
                                    _jobExistsHandler.Handle(new DoesJobExistQuery<IDbConnection, IDbTransaction>(jobName, scheduledTime, connection,
                                        trans)) ==
                                    QueueStatuses.NotQueued)
                                {
                                    command.Transaction = trans;
                                    await _readerAsync.ExecuteNonQueryAsync(command).ConfigureAwait(false);
                                    var commandId = connection.CreateCommand();
                                    commandId.Transaction = trans;
                                    commandId.CommandText = "SELECT last_insert_rowid();";
                                    id = Convert.ToInt64(await _readerAsync.ExecuteScalarAsync(commandId).ConfigureAwait(false));
                                    if (id > 0)
                                    {
                                        commandMeta.Transaction = trans;
                                        var param = commandMeta.CreateParameter();
                                        param.ParameterName = "@QueueID";
                                        param.DbType = DbType.Int64;
                                        param.Value = id;
                                        commandMeta.Parameters.Add(param);
                                        await _readerAsync.ExecuteNonQueryAsync(commandMeta).ConfigureAwait(false);
                                        if (commandStatus != null)
                                        {
                                            commandStatus.Transaction = trans;

                                            param = commandStatus.CreateParameter();
                                            param.ParameterName = "@QueueID";
                                            param.DbType = DbType.Int64;
                                            param.Value = id;
                                            commandStatus.Parameters.Add(param);

                                            await _readerAsync.ExecuteNonQueryAsync(commandStatus).ConfigureAwait(false);
                                        }
                                        if (!string.IsNullOrWhiteSpace(jobName))
                                        {
                                            _sendJobStatus.Handle(new SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>(jobName, eventTime,
                                                scheduledTime, connection, trans));
                                        }
                                        trans.Commit();
                                    }
                                    else
                                    {
                                        throw new DotNetWorkQueueException(
                                            "Failed to insert record - the ID of the new record returned by SQLite was 0");
                                    }
                                }
                                else
                                {
                                    throw new DotNetWorkQueueException(
                                          "Failed to insert record - the job has already been queued or processed");
                                }
                            }
                            finally
                            {
                                commandStatus?.Dispose();
                            }
                        }
                    }
                    return id;
                }
            }
        }

        /// <summary>
        /// Creates the status record.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private IDbCommand CreateStatusRecord(IDbConnection connection, IMessage message,
            IAdditionalMessageData data)
        {
            var command = connection.CreateCommand();
            SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, 0, _options.Value, _getTime.GetCurrentUtcDate());
            return command;
        }
    }
}
