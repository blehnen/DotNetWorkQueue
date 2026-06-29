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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Asynchronous counterpart to <see cref="SendMessageCommandBatchHandler"/>. Sends a batch of
    /// messages as a true bulk insert in one transaction; whole-batch atomic. System.Data.SQLite
    /// has no real async IO, so the ADO calls run through <see cref="IReaderAsync"/> exactly like
    /// the single-message SQLite async handler.
    /// </summary>
    internal class SendMessageCommandBatchHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly IGetTime _getTime;
        private readonly IDbFactory _dbFactory;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IReaderAsync _readerAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandlerAsync"/> class.
        /// </summary>
        public SendMessageCommandBatchHandlerAsync(ITableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            TransportConfigurationSend configurationSend,
            IGetTimeFactory getTimeFactory,
            IDbFactory dbFactory,
            IJobSchedulerMetaData jobSchedulerMetaData,
            ISentMessageFactory sentMessageFactory,
            IReaderAsync readerAsync)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            Guard.NotNull(() => dbFactory, dbFactory);
            Guard.NotNull(() => jobSchedulerMetaData, jobSchedulerMetaData);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => readerAsync, readerAsync);

            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _configurationSend = configurationSend;
            _getTime = getTimeFactory.Create();
            _dbFactory = dbFactory;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _sentMessageFactory = sentMessageFactory;
            _readerAsync = readerAsync;
        }

        /// <inheritdoc />
        public async Task<QueueOutputMessages> HandleAsync(SendMessageCommandBatch command)
        {
            Guard.NotNull(() => command, command);
            var messages = command.Messages;
            if (messages.Count == 0)
                return new QueueOutputMessages(new List<IQueueOutputMessage>());

            BatchSendValidation.GuardNoScheduledJobs(messages, _jobSchedulerMetaData);

            var options = _options.Value;
            var batchSizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize,
                options.BatchSize > 0 ? options.BatchSize : (int?)null);

            var results = new IQueueOutputMessage[messages.Count];
            using (var connection = _dbFactory.CreateConnection(_configurationSend.ConnectionInfo.ConnectionString, false))
            {
                connection.Open();
                using (var trans = _dbFactory.CreateTransaction(connection).BeginTransaction())
                {
                    try
                    {
                        var globalIndex = 0;
                        foreach (var chunk in messages.Partition(batchSizer.BatchSize(messages.Count)))
                        {
                            globalIndex = await ProcessChunkAsync(connection, trans, chunk, options, results, globalIndex)
                                .ConfigureAwait(false);
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

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private async Task<int> ProcessChunkAsync(IDbConnection connection, IDbTransaction trans,
            IReadOnlyList<QueueMessage<IMessage, IAdditionalMessageData>> chunk,
            SqLiteMessageQueueTransportOptions options,
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

            // 2. Multi-row body insert; recover generated ids in input order via ascending sort.
            var ids = new long[chunk.Count];
            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                SendMessageBatch.BuildBodyInsertReturningCommand(command, _tableNameHelper, rows);
                var returned = new List<long>(chunk.Count);
                using (var reader = await _readerAsync.ExecuteReaderAsync(command).ConfigureAwait(false))
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

                using (var commandMeta = SendMessage.CreateMetaDataRecord(m.MessageData.GetDelay(), expiration,
                    connection, m.Message, m.MessageData, _tableNameHelper, _headers, options, _getTime))
                {
                    commandMeta.Transaction = trans;
                    var param = commandMeta.CreateParameter();
                    param.ParameterName = "@QueueID";
                    param.DbType = DbType.Int64;
                    param.Value = id;
                    commandMeta.Parameters.Add(param);
                    await _readerAsync.ExecuteNonQueryAsync(commandMeta).ConfigureAwait(false);
                }

                if (options.EnableStatusTable)
                {
                    using (var commandStatus = connection.CreateCommand())
                    {
                        SendMessage.BuildStatusCommand(commandStatus, _tableNameHelper, _headers,
                            m.MessageData, m.Message, id, options, _getTime.GetCurrentUtcDate());
                        commandStatus.Transaction = trans;
                        await _readerAsync.ExecuteNonQueryAsync(commandStatus).ConfigureAwait(false);
                    }
                }

                results[globalIndex++] = new QueueOutputMessage(
                    _sentMessageFactory.Create(new MessageQueueId<long>(id), m.MessageData.CorrelationId));
            }

            return globalIndex;
        }
    }
}
