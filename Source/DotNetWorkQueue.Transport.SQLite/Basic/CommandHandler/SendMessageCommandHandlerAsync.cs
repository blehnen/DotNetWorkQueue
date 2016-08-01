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
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommand, long>
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly IGetTime _getTime;
        private readonly ISqLiteTransactionFactory _transactionFactory;
        private readonly ICommandHandler<SetJobLastKnownEventCommand> _sendJobStatus;
        private readonly IQueryHandler<DoesJobExistQuery, QueueStatuses> _jobExistsHandler;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;

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
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="sendJobStatus">The send job status.</param>
        /// <param name="jobExistsHandler">The job exists handler.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        public SendMessageCommandHandlerAsync(TableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            SqLiteCommandStringCache commandCache, 
            TransportConfigurationSend configurationSend,
            IGetTimeFactory getTimeFactory,
            ISqLiteTransactionFactory transactionFactory,
            ICommandHandler<SetJobLastKnownEventCommand> sendJobStatus, 
            IQueryHandler<DoesJobExistQuery, QueueStatuses> jobExistsHandler, 
            IJobSchedulerMetaData jobSchedulerMetaData)
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

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _commandCache = commandCache;
            _configurationSend = configurationSend;
            _getTime = getTimeFactory.Create();
            _transactionFactory = transactionFactory;
            _sendJobStatus = sendJobStatus;
            _jobExistsHandler = jobExistsHandler;
            _jobSchedulerMetaData = jobSchedulerMetaData;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="commandSend">The command.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to insert record - the ID of the new record returned by SQLite was 0</exception>
        public async Task<long> Handle(SendMessageCommand commandSend)
        {
            if (!DatabaseExists.Exists(_configurationSend.ConnectionInfo.ConnectionString))
            {
                return 0;
            }

            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration ||
                                            _options.Value.QueueType == QueueTypes.RpcReceive ||
                                            _options.Value.QueueType == QueueTypes.RpcSend;
            }

            using (var connection = new SQLiteConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                connection.Open();

                var expiration = TimeSpan.Zero;
                if (_messageExpirationEnabled.Value)
                {
                    expiration = MessageExpiration.GetExpiration(commandSend, _headers);
                }

                var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
                var scheduledTime = DateTimeOffset.MinValue;
                var eventTime = DateTimeOffset.MinValue;
                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                    eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
                }

                SQLiteCommand commandStatus = null;
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

                        using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(jobName) || 
                                    _jobExistsHandler.Handle(new DoesJobExistQuery(jobName, scheduledTime, connection,
                                        trans)) ==
                                    QueueStatuses.NotQueued)
                                {
                                    command.Transaction = trans;
                                    id = Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false));
                                    if (id > 0)
                                    {
                                        commandMeta.Transaction = trans;
                                        commandMeta.Parameters.Add("@QueueID", DbType.Int64, 8).Value = id;
                                        await commandMeta.ExecuteNonQueryAsync().ConfigureAwait(false);
                                        if (commandStatus != null)
                                        {
                                            commandStatus.Transaction = trans;
                                            commandStatus.Parameters.Add("@QueueID", DbType.Int64, 8).Value = id;
                                            await commandStatus.ExecuteNonQueryAsync().ConfigureAwait(false);
                                        }
                                        if (!string.IsNullOrWhiteSpace(jobName))
                                        {
                                            _sendJobStatus.Handle(new SetJobLastKnownEventCommand(jobName, eventTime,
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
        private SQLiteCommand CreateStatusRecord(SQLiteConnection connection, IMessage message,
            IAdditionalMessageData data)
        {
            var command = connection.CreateCommand();
            SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, 0, _options.Value, _getTime.GetCurrentUtcDate());
            return command;
        }
    }
}
