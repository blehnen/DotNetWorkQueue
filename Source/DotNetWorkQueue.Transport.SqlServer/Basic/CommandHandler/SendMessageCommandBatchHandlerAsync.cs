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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Asynchronous counterpart to <see cref="SendMessageCommandBatchHandler"/>. Sends a batch of
    /// messages as a true bulk insert in one transaction; whole-batch atomic.
    /// </summary>
    internal class SendMessageCommandBatchHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly ISentMessageFactory _sentMessageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandlerAsync"/> class.
        /// </summary>
        public SendMessageCommandBatchHandlerAsync(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            TransportConfigurationSend configurationSend,
            IJobSchedulerMetaData jobSchedulerMetaData,
            ISentMessageFactory sentMessageFactory)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => jobSchedulerMetaData, jobSchedulerMetaData);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _configurationSend = configurationSend;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _sentMessageFactory = sentMessageFactory;
        }

        /// <inheritdoc />
        public async Task<QueueOutputMessages> HandleAsync(SendMessageCommandBatch command)
        {
            Guard.NotNull(() => command, command);
            var messages = command.Messages;
            if (messages.Count == 0)
                return new QueueOutputMessages(new List<IQueueOutputMessage>());

            BatchSendValidation.GuardNoScheduledJobs(messages, _jobSchedulerMetaData);

            // Inbox (held-transaction) batch path: when the caller supplied a transaction, reuse
            // its connection/transaction instead of opening our own (and never commit it).
            if (command is RelationalSendMessageCommandBatch rel && rel.ExternalTransaction != null)
                return await HandleExternalTransactionAsync(rel).ConfigureAwait(false);

            var options = _options.Value;
            var batchSizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize,
                options.BatchSize > 0 ? options.BatchSize : (int?)null);

            var results = new IQueueOutputMessage[messages.Count];
            using (var connection = new SqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var trans = (SqlTransaction)await connection.BeginTransactionAsync().ConfigureAwait(false))
                {
                    try
                    {
                        var globalIndex = 0;
                        foreach (var chunk in messages.Partition(batchSizer.BatchSize(messages.Count)))
                        {
                            globalIndex = await ProcessChunkAsync(connection, trans, chunk, options, results, globalIndex)
                                .ConfigureAwait(false);
                        }
                        await trans.CommitAsync().ConfigureAwait(false);
                    }
                    catch (Exception error)
                    {
                        try
                        {
                            await trans.RollbackAsync().ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            // Preserve the original failure as the reported error; a rollback
                            // failure (e.g. a dropped connection) must not mask it. The using
                            // block disposes the transaction regardless.
                        }
                        // Whole-batch atomic: every message reports the failure.
                        return new QueueOutputMessages(messages
                            .Select(m => (IQueueOutputMessage)new QueueOutputMessage(
                                _sentMessageFactory.Create(null, m.MessageData.CorrelationId), error))
                            .ToList());
                    }
                }
            }
            return new QueueOutputMessages(results.ToList());
        }

        /// <summary>
        /// Inbox (held-transaction) batch path. Runs the same multi-row body insert and per-message
        /// meta/status rows as <see cref="HandleAsync"/>, but on the caller-supplied connection and
        /// transaction. The caller owns the commit/rollback, so this method opens nothing, commits
        /// nothing, and wraps nothing — any failure propagates so the caller can roll back.
        /// </summary>
        private async Task<QueueOutputMessages> HandleExternalTransactionAsync(RelationalSendMessageCommandBatch rel)
        {
            var sqlTransaction = (SqlTransaction)rel.ExternalTransaction;
            var sqlConn = sqlTransaction.Connection;

            var messages = rel.Messages;
            var options = _options.Value;
            var batchSizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize,
                options.BatchSize > 0 ? options.BatchSize : (int?)null);

            var results = new IQueueOutputMessage[messages.Count];
            var globalIndex = 0;
            foreach (var chunk in messages.Partition(batchSizer.BatchSize(messages.Count)))
            {
                globalIndex = await ProcessChunkAsync(sqlConn, sqlTransaction, chunk, options, results, globalIndex)
                    .ConfigureAwait(false);
            }

            // Caller owns the transaction lifecycle — deliberately no Commit/Rollback/Dispose/Close
            // and no try/catch wrapper (failures propagate so the caller can roll back).
            return new QueueOutputMessages(results.ToList());
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private async Task<int> ProcessChunkAsync(SqlConnection connection, SqlTransaction trans,
            IReadOnlyList<QueueMessage<IMessage, IAdditionalMessageData>> chunk,
            SqlServerMessageQueueTransportOptions options,
            IQueueOutputMessage[] results, int globalIndex)
        {
            // 1. Serialize each message's body, then its headers (with the interceptor graph header set).
            var rows = new List<(byte[] Body, byte[] Headers)>(chunk.Count);
            foreach (var m in chunk)
            {
                var serialization = _serializer.Serializer.MessageToBytes(
                    new MessageBody { Body = m.Message.Body }, m.Message.Headers);
                m.Message.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);
                var headerBytes = _serializer.InternalSerializer.ConvertToBytes(m.Message.Headers);
                rows.Add((serialization.Output, headerBytes));
            }

            // 2. Multi-row body insert; recover generated ids in input order via the ordinal.
            var ids = new long[chunk.Count];
            var assigned = new bool[chunk.Count];
            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                SendMessageBatch.BuildBodyMergeCommand(command, _tableNameHelper, rows);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var id = reader.GetInt64(0);
                        var ordinal = reader.GetInt32(1);
                        ids[ordinal] = id;
                        assigned[ordinal] = true;
                    }
                }
            }
            for (var i = 0; i < ids.Length; i++)
            {
                if (!assigned[i] || ids[i] <= 0)
                    throw new DotNetWorkQueueException(
                        "Failed to insert record - the batch body insert did not return an id for every message");
            }

            // 3. Per-message meta (and optional status) rows, in the same transaction.
            for (var i = 0; i < chunk.Count; i++)
            {
                var m = chunk[i];
                var id = ids[i];
                var expiration = TimeSpan.Zero;
                if (options.EnableMessageExpiration)
                {
                    expiration = MessageExpiration.GetExpiration(
                        new SendMessageCommand(m.Message, m.MessageData), data => data.GetExpiration());
                }

                await CreateMetaDataRecordAsync(m.MessageData.GetDelay(), expiration, connection, id,
                    m.Message, m.MessageData, trans, options).ConfigureAwait(false);
                if (options.EnableStatusTable)
                {
                    await CreateStatusRecordAsync(connection, id, m.Message, m.MessageData, trans, options)
                        .ConfigureAwait(false);
                }

                results[globalIndex++] = new QueueOutputMessage(
                    _sentMessageFactory.Create(new MessageQueueId<long>(id), m.MessageData.CorrelationId));
            }

            return globalIndex;
        }

        private async Task CreateStatusRecordAsync(SqlConnection connection, long id, IMessage message,
            IAdditionalMessageData data, SqlTransaction trans, SqlServerMessageQueueTransportOptions options)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, id, options);
                command.Transaction = trans;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task CreateMetaDataRecordAsync(TimeSpan? delay, TimeSpan expiration, SqlConnection connection, long id,
            IMessage message, IAdditionalMessageData data, SqlTransaction trans, SqlServerMessageQueueTransportOptions options)
        {
            using (var command = connection.CreateCommand())
            {
                SendMessage.BuildMetaCommand(command, _tableNameHelper, _headers, data, message, id, options, delay, expiration);
                command.Transaction = trans;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
