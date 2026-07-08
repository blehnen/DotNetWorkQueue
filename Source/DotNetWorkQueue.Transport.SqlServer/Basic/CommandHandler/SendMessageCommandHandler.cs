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
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
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

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, long>
    {
        private const string BodyParameter = "@body";
        private const string HeadersParameter = "@headers";
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>> _sendJobStatus;
        private readonly IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> _jobExistsHandler;
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
        /// <param name="sendJobStatus">The send job status.</param>
        /// <param name="jobExistsHandler">The job exists handler.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        public SendMessageCommandHandler(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            SqlServerCommandStringCache commandCache,
            TransportConfigurationSend configurationSend,
            ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>> sendJobStatus, IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> jobExistsHandler,
            IJobSchedulerMetaData jobSchedulerMetaData)
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

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _commandCache = commandCache;
            _configurationSend = configurationSend;
            _sendJobStatus = sendJobStatus;
            _jobExistsHandler = jobExistsHandler;
            _jobSchedulerMetaData = jobSchedulerMetaData;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="commandSend">The command.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to insert record - the ID of the new record returned by SQL server was 0</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        public long Handle(SendMessageCommand commandSend)
        {
            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
            }

            if (commandSend is RelationalSendMessageCommand relCommand && relCommand.ExternalTransaction != null)
                return HandleExternalTransaction(commandSend);

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            using (var connection = new SqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    if (string.IsNullOrWhiteSpace(jobName) || _jobExistsHandler.Handle(new DoesJobExistQuery<SqlConnection, SqlTransaction>(jobName, scheduledTime, connection, trans)) ==
                        QueueStatuses.NotQueued)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = trans;
                            command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                            var serialization =
                                _serializer.Serializer.MessageToBytes(new MessageBody
                                {
                                    Body = commandSend.MessageToSend.Body
                                }, commandSend.MessageToSend.Headers);

                            command.Parameters.Add(BodyParameter, SqlDbType.VarBinary, -1);
                            command.Parameters[BodyParameter].Value = serialization.Output;

                            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph,
                                serialization.Graph);

                            command.Parameters.Add(HeadersParameter, SqlDbType.VarBinary, -1);
                            command.Parameters[HeadersParameter].Value =
                                _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                            var id = Convert.ToInt64(command.ExecuteScalar());
                            if (id > 0)
                            {
                                var expiration = TimeSpan.Zero;
                                if (_messageExpirationEnabled.Value)
                                {
                                    expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
                                }

                                CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration, connection, id,
                                    commandSend.MessageToSend, commandSend.MessageData, trans);
                                if (_options.Value.EnableStatusTable)
                                {
                                    CreateStatusRecord(connection, id, commandSend.MessageToSend,
                                        commandSend.MessageData, trans);
                                }

                                if (!string.IsNullOrWhiteSpace(jobName))
                                {
                                    _sendJobStatus.Handle(new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(jobName, eventTime,
                                        scheduledTime, connection, trans));
                                }
                            }
                            else
                            {
                                throw new DotNetWorkQueueException(
                                    "Failed to insert record - the ID of the new record returned by SQL server was 0");
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
        /// Caller-supplied-transaction fork of <see cref="Handle(SendMessageCommand)"/>. Reuses
        /// the caller's <see cref="SqlTransaction"/> and its <see cref="SqlConnection"/> for all
        /// queue INSERTs; never commits, rolls back, closes, or disposes the caller's resources.
        /// Invoked from <see cref="Handle"/> when <see cref="RelationalSendMessageCommand.ExternalTransaction"/>
        /// is non-null. The producer surface (<c>SqlServerRelationalProducerQueue&lt;T&gt;</c>)
        /// guarantees the transaction is a <see cref="SqlTransaction"/> and its connection's
        /// database matches the queue's configured database via the validator at the API boundary,
        /// so this method performs no validation of its own.
        /// </summary>
        /// <param name="commandSend">The send-message command carrying a non-null
        /// <see cref="RelationalSendMessageCommand.ExternalTransaction"/>.</param>
        /// <returns>The newly-inserted message ID.</returns>
        /// <exception cref="DotNetWorkQueueException">Thrown when the INSERT returns a zero ID
        /// or when the job-uniqueness query rejects the command.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private long HandleExternalTransaction(SendMessageCommand commandSend)
        {
            // Cast guard: Handle() only routes here for RelationalSendMessageCommand (the
            // `commandSend is RelationalSendMessageCommand rel` pattern in the fork check),
            // so this cast is always safe. The producer subclass also enforces the
            // DbTransaction subtype via GuardSqlTransaction before construction.
            var relCommand = (RelationalSendMessageCommand)commandSend;
            var sqlTransaction = (SqlTransaction)relCommand.ExternalTransaction;
            var sqlConn = (SqlConnection)sqlTransaction.Connection;

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            if (!(string.IsNullOrWhiteSpace(jobName) ||
                  _jobExistsHandler.Handle(new DoesJobExistQuery<SqlConnection, SqlTransaction>(
                      jobName, scheduledTime, sqlConn, sqlTransaction)) == QueueStatuses.NotQueued))
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the job has already been queued or processed");
            }

            long id;
            using (var command = sqlConn.CreateCommand())
            {
                command.Connection = sqlConn;
                command.Transaction = sqlTransaction;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                var serialization = _serializer.Serializer.MessageToBytes(
                    new MessageBody { Body = commandSend.MessageToSend.Body },
                    commandSend.MessageToSend.Headers);

                command.Parameters.Add(BodyParameter, SqlDbType.VarBinary, -1);
                command.Parameters[BodyParameter].Value = serialization.Output;

                commandSend.MessageToSend.SetHeader(
                    _headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);

                command.Parameters.Add(HeadersParameter, SqlDbType.VarBinary, -1);
                command.Parameters[HeadersParameter].Value =
                    _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                id = Convert.ToInt64(command.ExecuteScalar());
            }

            if (id <= 0)
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the ID of the new record returned by SQL server was 0");
            }

            var expiration = TimeSpan.Zero;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration, sqlConn, id,
                commandSend.MessageToSend, commandSend.MessageData, sqlTransaction);

            if (_options.Value.EnableStatusTable)
            {
                CreateStatusRecord(sqlConn, id, commandSend.MessageToSend, commandSend.MessageData, sqlTransaction);
            }

            if (!string.IsNullOrWhiteSpace(jobName))
            {
                _sendJobStatus.Handle(new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(
                    jobName, eventTime, scheduledTime, sqlConn, sqlTransaction));
            }

            // Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().
            // The caller owns the transaction lifecycle.
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
        private void CreateStatusRecord(SqlConnection connection, long id, IMessage message,
            IAdditionalMessageData data, SqlTransaction trans)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, id, _options.Value);
                command.Transaction = trans;
                command.ExecuteNonQuery();
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
        private void CreateMetaDataRecord(TimeSpan? delay, TimeSpan expiration, SqlConnection connection, long id, IMessage message, IAdditionalMessageData data,
            SqlTransaction trans)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildMetaCommand(command, _tableNameHelper, _headers,
                   data, message, id, _options.Value, delay, expiration);
                command.Transaction = trans;
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}
