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
using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommand, long>
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>> _sendJobStatus;
        private readonly IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> _jobExistsHandler;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configurationSend">The configuration send.</param>
        /// <param name="sendJobStatus">The send job status.</param>
        /// <param name="jobExistsHandler">The job exists handler.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SendMessageCommandHandlerAsync(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            IPostgreSqlMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            PostgreSqlCommandStringCache commandCache,
            TransportConfigurationSend configurationSend,
            ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>> sendJobStatus, IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> jobExistsHandler,
            IJobSchedulerMetaData jobSchedulerMetaData,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => sendJobStatus, sendJobStatus);
            Guard.NotNull(() => jobExistsHandler, jobExistsHandler);
            Guard.NotNull(() => jobSchedulerMetaData, jobSchedulerMetaData);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _commandCache = commandCache;
            _configurationSend = configurationSend;
            _sendJobStatus = sendJobStatus;
            _jobExistsHandler = jobExistsHandler;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _getTime = getTimeFactory.Create();
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(SendMessageCommand commandSend)
        {
            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
            }

            if (commandSend is RelationalSendMessageCommand relCommand && relCommand.ExternalTransaction != null)
                return await HandleExternalTransactionAsync(commandSend).ConfigureAwait(false);

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            using (var connection = new NpgsqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    if (string.IsNullOrWhiteSpace(jobName) || _jobExistsHandler.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(jobName, scheduledTime, connection, trans)) ==
                        QueueStatuses.NotQueued)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                            command.Transaction = trans;
                            var serialization =
                                _serializer.Serializer.MessageToBytes(new MessageBody
                                {
                                    Body = commandSend.MessageToSend.Body
                                }, commandSend.MessageToSend.Headers);

                            command.Parameters.Add("@body", NpgsqlDbType.Bytea, -1);
                            command.Parameters["@body"].Value = serialization.Output;

                            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph,
                                serialization.Graph);

                            command.Parameters.Add("@headers", NpgsqlDbType.Bytea, -1);
                            command.Parameters["@headers"].Value =
                                _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                            var id = Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false));
                            if (id > 0)
                            {
                                var expiration = TimeSpan.Zero;
                                if (_messageExpirationEnabled.Value)
                                {
                                    expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
                                }

                                await
                                    CreateMetaDataRecordAsync(commandSend.MessageData.GetDelay(), expiration, connection,
                                        id,
                                        commandSend.MessageToSend, commandSend.MessageData, trans, _getTime.GetCurrentUtcDate()).ConfigureAwait(false);
                                if (_options.Value.EnableStatusTable)
                                {
                                    await
                                        CreateStatusRecordAsync(connection, id, commandSend.MessageToSend,
                                            commandSend.MessageData, trans).ConfigureAwait(false);
                                }

                                if (!string.IsNullOrWhiteSpace(jobName))
                                {
                                    _sendJobStatus.Handle(new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(jobName, eventTime,
                                        scheduledTime, connection, trans));
                                }

                            }
                            else
                            {
                                throw new DotNetWorkQueueException(
                                    "Failed to insert record - the ID of the new record returned by the server was 0");
                            }
                            trans.Commit();
                            return id;
                        }
                    }
                    throw new DotNetWorkQueueException(
                        "Failed to insert record - the job has already been queued or processed");
                }
            }
        }

        /// <summary>
        /// Async caller-supplied-transaction fork of <see cref="HandleAsync(SendMessageCommand)"/>.
        /// Reuses the caller's <see cref="NpgsqlTransaction"/> and its
        /// <see cref="NpgsqlConnection"/> for all queue INSERTs; never commits, rolls back,
        /// closes, or disposes the caller's resources. Invoked from <see cref="HandleAsync"/>
        /// when <see cref="RelationalSendMessageCommand.ExternalTransaction"/> is non-null. The producer
        /// surface (<c>PostgreSqlRelationalProducerQueue&lt;T&gt;</c>) validates the
        /// transaction at the API boundary, so this method performs no validation of its own.
        /// </summary>
        /// <param name="commandSend">The send-message command carrying a non-null
        /// <see cref="RelationalSendMessageCommand.ExternalTransaction"/>.</param>
        /// <returns>The newly-inserted message ID.</returns>
        /// <exception cref="DotNetWorkQueueException">Thrown when the INSERT returns a zero
        /// ID or when the job-uniqueness query rejects the command.</exception>
        private async Task<long> HandleExternalTransactionAsync(SendMessageCommand commandSend)
        {
            // Cast guard: HandleAsync() only routes here for RelationalSendMessageCommand
            // (the `commandSend is RelationalSendMessageCommand rel` pattern in the fork
            // check), so this cast is always safe. The producer subclass also enforces
            // the DbTransaction subtype via GuardNpgsqlTransaction before construction.
            var relCommand = (RelationalSendMessageCommand)commandSend;
            var npgsqlTransaction = (NpgsqlTransaction)relCommand.ExternalTransaction;
            var npgsqlConn = (NpgsqlConnection)npgsqlTransaction.Connection;

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            // Job-uniqueness query is sync on this transport (no async overload exists; the
            // existing self-managed-transaction async path also calls .Handle() synchronously — see
            // SendMessageCommandHandlerAsync.cs line ~122 in the pre-Phase-4 baseline).
            if (!(string.IsNullOrWhiteSpace(jobName) ||
                  _jobExistsHandler.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(
                      jobName, scheduledTime, npgsqlConn, npgsqlTransaction)) == QueueStatuses.NotQueued))
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the job has already been queued or processed");
            }

            long id;
            using (var command = npgsqlConn.CreateCommand())
            {
                command.Transaction = npgsqlTransaction;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                var serialization = _serializer.Serializer.MessageToBytes(
                    new MessageBody { Body = commandSend.MessageToSend.Body },
                    commandSend.MessageToSend.Headers);

                command.Parameters.Add("@body", NpgsqlDbType.Bytea, -1);
                command.Parameters["@body"].Value = serialization.Output;

                commandSend.MessageToSend.SetHeader(
                    _headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);

                command.Parameters.Add("@headers", NpgsqlDbType.Bytea, -1);
                command.Parameters["@headers"].Value =
                    _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                id = Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false));
            }

            if (id <= 0)
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the ID of the new record returned by the server was 0");
            }

            var expiration = TimeSpan.Zero;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            // PG-specific: CreateMetaDataRecordAsync takes a DateTime currentTime as the
            // eighth argument. IGetTime.GetCurrentUtcDate() is synchronous — invoke directly,
            // no await needed.
            await CreateMetaDataRecordAsync(commandSend.MessageData.GetDelay(), expiration,
                npgsqlConn, id, commandSend.MessageToSend, commandSend.MessageData, npgsqlTransaction,
                _getTime.GetCurrentUtcDate()).ConfigureAwait(false);

            if (_options.Value.EnableStatusTable)
            {
                await CreateStatusRecordAsync(npgsqlConn, id, commandSend.MessageToSend,
                    commandSend.MessageData, npgsqlTransaction).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(jobName))
            {
                _sendJobStatus.Handle(new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(
                    jobName, eventTime, scheduledTime, npgsqlConn, npgsqlTransaction));
            }

            // Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.
            return id;
        }

        /// <summary>
        /// Creates the status record.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <param name="trans">The transaction.</param>
        /// <returns></returns>
        private async Task CreateStatusRecordAsync(NpgsqlConnection connection, long id, IMessage message,
            IAdditionalMessageData data, NpgsqlTransaction trans)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, id, _options.Value);
                command.Transaction = trans;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        #region Insert Meta data record

        /// <summary>
        /// Creates the meta data record.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="expiration">The expiration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        /// <param name="trans">The transaction.</param>
        /// <param name="currentTime">The current time.</param>
        /// <returns></returns>
        private async Task CreateMetaDataRecordAsync(TimeSpan? delay, TimeSpan expiration, NpgsqlConnection connection, long id,
            IMessage message, IAdditionalMessageData data, NpgsqlTransaction trans, DateTime currentTime)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildMetaCommand(command, _tableNameHelper, _headers,
                    data, message, id, _options.Value, delay, expiration, currentTime);
                command.Transaction = trans;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        #endregion 
    }
}
