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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Sends a batch of messages to the queue as a true bulk insert: one connection, one
    /// transaction spanning all chunks, a multi-row <c>INSERT … unnest … RETURNING queueid</c>
    /// body insert per chunk (generated ids recovered in input order by ascending sort), and
    /// per-message meta/status inserts. The whole batch is atomic — any failure rolls back every
    /// row and each returned result reports the failure.
    /// </summary>
    /// <remarks>
    /// Scheduled-job messages are not supported on the batch path (the per-message job-uniqueness
    /// query has no batch equivalent); send those individually via <c>Send(message)</c>. Only the
    /// body insert is multi-row because the body columns are uniform; meta and status rows are
    /// inserted per message because their columns can vary per message (user metadata).
    /// </remarks>
    internal class SendMessageCommandBatchHandler : ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly IGetTime _getTime;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly ISentMessageFactory _sentMessageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandler"/> class.
        /// </summary>
        public SendMessageCommandBatchHandler(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            IPostgreSqlMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            TransportConfigurationSend configurationSend,
            IGetTimeFactory getTimeFactory,
            IJobSchedulerMetaData jobSchedulerMetaData,
            ISentMessageFactory sentMessageFactory)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            Guard.NotNull(() => jobSchedulerMetaData, jobSchedulerMetaData);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _configurationSend = configurationSend;
            _getTime = getTimeFactory.Create();
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _sentMessageFactory = sentMessageFactory;
        }

        /// <inheritdoc />
        public QueueOutputMessages Handle(SendMessageCommandBatch command)
        {
            Guard.NotNull(() => command, command);
            var messages = command.Messages;
            if (messages.Count == 0)
                return new QueueOutputMessages(new List<IQueueOutputMessage>());

            BatchSendValidation.GuardNoScheduledJobs(messages, _jobSchedulerMetaData);

            // Inbox (held-transaction) batch path: when the caller supplied a transaction, reuse
            // its connection/transaction instead of opening our own (and never commit it).
            if (command is RelationalSendMessageCommandBatch rel && rel.ExternalTransaction != null)
                return HandleExternalTransaction(rel);

            var options = _options.Value;
            var batchSizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize,
                options.BatchSize > 0 ? options.BatchSize : (int?)null);

            var results = new IQueueOutputMessage[messages.Count];
            using (var connection = new NpgsqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    try
                    {
                        var globalIndex = 0;
                        foreach (var chunk in messages.Partition(batchSizer.BatchSize(messages.Count)))
                        {
                            ProcessChunk(connection, trans, chunk, options, results, ref globalIndex);
                        }
                        trans.Commit();
                    }
                    catch (Exception error)
                    {
                        try
                        {
                            trans.Rollback();
                        }
                        catch (Exception)
                        {
                            // Preserve the original failure as the reported error; a rollback
                            // failure must not mask it. The using block disposes the transaction.
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
        /// meta/status rows as <see cref="Handle"/>, but on the caller-supplied connection and
        /// transaction. The caller owns the commit/rollback, so this method opens nothing, commits
        /// nothing, and wraps nothing — any failure propagates so the caller can roll back.
        /// </summary>
        private QueueOutputMessages HandleExternalTransaction(RelationalSendMessageCommandBatch rel)
        {
            var npgsqlTransaction = (NpgsqlTransaction)rel.ExternalTransaction;
            var npgsqlConn = (NpgsqlConnection)npgsqlTransaction.Connection;

            var messages = rel.Messages;
            var options = _options.Value;
            var batchSizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize,
                options.BatchSize > 0 ? options.BatchSize : (int?)null);

            var results = new IQueueOutputMessage[messages.Count];
            var globalIndex = 0;
            foreach (var chunk in messages.Partition(batchSizer.BatchSize(messages.Count)))
            {
                ProcessChunk(npgsqlConn, npgsqlTransaction, chunk, options, results, ref globalIndex);
            }

            // Caller owns the transaction lifecycle — deliberately no Commit/Rollback/Dispose/Close
            // and no try/catch wrapper (failures propagate so the caller can roll back).
            return new QueueOutputMessages(results.ToList());
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private void ProcessChunk(NpgsqlConnection connection, NpgsqlTransaction trans,
            IReadOnlyList<QueueMessage<IMessage, IAdditionalMessageData>> chunk,
            PostgreSqlMessageQueueTransportOptions options,
            IQueueOutputMessage[] results, ref int globalIndex)
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

            // 2. Multi-row body insert; recover generated ids in input order via ascending sort
            //    (queueid is bigserial assigned in ORDER BY ord order within this transaction).
            var ids = new long[chunk.Count];
            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                SendMessageBatch.BuildBodyInsertReturningCommand(command, _tableNameHelper, rows);
                var returned = new List<long>(chunk.Count);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        returned.Add(reader.GetInt64(0));
                }
                if (returned.Count != chunk.Count)
                    throw new DotNetWorkQueueException(
                        "Failed to insert record - the batch body insert did not return an id for every message");
                returned.Sort();
                for (var i = 0; i < ids.Length; i++)
                    ids[i] = returned[i];
            }
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] <= 0)
                    throw new DotNetWorkQueueException(
                        "Failed to insert record - the batch body insert returned an invalid id");
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

                using (var commandMeta = connection.CreateCommand())
                {
                    SendMessage.BuildMetaCommand(commandMeta, _tableNameHelper, _headers, m.MessageData, m.Message,
                        id, options, m.MessageData.GetDelay(), expiration, _getTime.GetCurrentUtcDate());
                    commandMeta.Transaction = trans;
                    commandMeta.ExecuteNonQuery();
                }

                if (options.EnableStatusTable)
                {
                    using (var commandStatus = connection.CreateCommand())
                    {
                        SendMessage.BuildStatusCommand(commandStatus, _tableNameHelper, _headers,
                            m.MessageData, m.Message, id, options);
                        commandStatus.Transaction = trans;
                        commandStatus.ExecuteNonQuery();
                    }
                }

                results[globalIndex++] = new QueueOutputMessage(
                    _sentMessageFactory.Create(new MessageQueueId<long>(id), m.MessageData.CorrelationId));
            }
        }
    }
}
