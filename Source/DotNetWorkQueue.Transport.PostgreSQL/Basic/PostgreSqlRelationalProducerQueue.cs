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
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// PostgreSQL-specific <see cref="RelationalProducerQueue{T}"/> that overrides the
    /// four caller-supplied-transaction hooks to dispatch
    /// <see cref="RelationalSendMessageCommand"/> instances through the registered
    /// PostgreSQL <c>SendMessageCommandHandler</c> / <c>SendMessageCommandHandlerAsync</c>.
    /// Validates the caller's transaction at the producer surface (fail-fast,
    /// boundary-checked) before any handler dispatch. Batch overrides iterate
    /// sequentially because ADO.NET transactions are not thread-safe.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public sealed class PostgreSqlRelationalProducerQueue<TMessage>
        : RelationalProducerQueue<TMessage>
        where TMessage : class
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendHandler;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendHandlerAsync;
        private readonly ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> _sendBatchHandler;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages> _sendBatchHandlerAsync;
        private readonly ExternalTransactionValidator _validator;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IMessageFactory _messageFactory;
        private readonly GenerateMessageHeaders _generateMessageHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlRelationalProducerQueue{TMessage}"/> class.
        /// </summary>
        /// <param name="configuration">Producer configuration.</param>
        /// <param name="sendMessages">Send-messages orchestrator (used by the inherited non-transaction path).</param>
        /// <param name="messageFactory">Message factory for the base class (non-transaction path).</param>
        /// <param name="log">Logger.</param>
        /// <param name="generateMessageHeaders">Standard header generator.</param>
        /// <param name="addStandardMessageHeaders">Standard header populator.</param>
        /// <param name="sendHandler">Registered sync handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="sendHandlerAsync">Registered async handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="sendBatchHandler">Registered sync handler for <see cref="SendMessageCommandBatch"/>
        /// (used by the held-transaction batch path; the decorated handler is correct because
        /// <see cref="RelationalSendMessageCommandBatch.SkipRetry"/> keeps the retry decorator out of
        /// the caller's transaction).</param>
        /// <param name="sendBatchHandlerAsync">Registered async handler for <see cref="SendMessageCommandBatch"/>.</param>
        /// <param name="validator">External-transaction validator (runs at the API boundary).</param>
        /// <param name="sentMessageFactory">Factory for the <see cref="ISentMessage"/> returned to callers.</param>
        /// <param name="ownMessageFactory">Same <see cref="IMessageFactory"/> instance retained for the
        /// caller-transaction path; re-injected because the base type seals its own copy as private.</param>
        public PostgreSqlRelationalProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log,
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders,
            ICommandHandlerWithOutput<SendMessageCommand, long> sendHandler,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendHandlerAsync,
            ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> sendBatchHandler,
            ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages> sendBatchHandlerAsync,
            ExternalTransactionValidator validator,
            ISentMessageFactory sentMessageFactory,
            IMessageFactory ownMessageFactory)
            : base(configuration, sendMessages, messageFactory, log,
                   generateMessageHeaders, addStandardMessageHeaders)
        {
            Guard.NotNull(() => sendHandler, sendHandler);
            Guard.NotNull(() => sendHandlerAsync, sendHandlerAsync);
            Guard.NotNull(() => sendBatchHandler, sendBatchHandler);
            Guard.NotNull(() => sendBatchHandlerAsync, sendBatchHandlerAsync);
            Guard.NotNull(() => validator, validator);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => ownMessageFactory, ownMessageFactory);
            _sendHandler = sendHandler;
            _sendHandlerAsync = sendHandlerAsync;
            _sendBatchHandler = sendBatchHandler;
            _sendBatchHandlerAsync = sendBatchHandlerAsync;
            _validator = validator;
            _sentMessageFactory = sentMessageFactory;
            _messageFactory = ownMessageFactory;
            _generateMessageHeaders = generateMessageHeaders;
        }

        /// <inheritdoc />
        protected override IQueueOutputMessage SendWithExternalTransaction(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            GuardNpgsqlTransaction(transaction);
            return SendOne(message, data ?? new AdditionalMessageData(), transaction);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessage> SendWithExternalTransactionAsync(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            GuardNpgsqlTransaction(transaction);
            return await SendOneAsync(message, data ?? new AdditionalMessageData(), transaction)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);   // ONCE, at the boundary (CONTEXT-4 Decision 4)
            GuardNpgsqlTransaction(transaction);
            // True multi-row insert inside the caller's transaction: one batch command, not a
            // SendOne loop. Exceptions propagate — the caller owns the rollback (PROJECT goal 3).
            return DispatchBatch(messages, transaction);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);
            GuardNpgsqlTransaction(transaction);
            return await DispatchBatchAsync(messages, transaction).ConfigureAwait(false);
        }

        // ----- private dispatch helpers -----

        /// <summary>
        /// Builds a single <see cref="RelationalSendMessageCommandBatch"/> carrying the caller's
        /// transaction and dispatches it to the batch handler. Internal (not private) so the
        /// build-and-dispatch behavior is unit-testable with a substitute <see cref="DbTransaction"/>;
        /// the <see cref="GuardNpgsqlTransaction"/> cast guard is covered separately because
        /// <see cref="Npgsql.NpgsqlTransaction"/> cannot be substituted without a live connection.
        /// </summary>
        internal IQueueOutputMessages DispatchBatch(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            var cmd = new RelationalSendMessageCommandBatch(BuildBatchMessages(messages), transaction);
            return _sendBatchHandler.Handle(cmd);
        }

        /// <summary>
        /// Async counterpart to <see cref="DispatchBatch"/>.
        /// </summary>
        internal async Task<IQueueOutputMessages> DispatchBatchAsync(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            var cmd = new RelationalSendMessageCommandBatch(BuildBatchMessages(messages), transaction);
            return await _sendBatchHandlerAsync.HandleAsync(cmd).ConfigureAwait(false);
        }

        private List<QueueMessage<IMessage, IAdditionalMessageData>> BuildBatchMessages(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages)
        {
            var imsgs = new List<QueueMessage<IMessage, IAdditionalMessageData>>(messages.Count);
            foreach (var m in messages)
            {
                var data = m.MessageData ?? new AdditionalMessageData();
                var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
                var imsg = _messageFactory.Create(m.Message, additionalHeaders);
                imsgs.Add(new QueueMessage<IMessage, IAdditionalMessageData>(imsg, data));
            }
            return imsgs;
        }

        private IQueueOutputMessage SendOne(TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
            var imsg = _messageFactory.Create(message, additionalHeaders);
            var cmd = new RelationalSendMessageCommand(imsg, data, transaction);
            var id = _sendHandler.Handle(cmd);
            return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<long>(id), data.CorrelationId));
        }

        private async Task<IQueueOutputMessage> SendOneAsync(TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            var additionalHeaders = _generateMessageHeaders.HeaderSetup(data);
            var imsg = _messageFactory.Create(message, additionalHeaders);
            var cmd = new RelationalSendMessageCommand(imsg, data, transaction);
            var id = await _sendHandlerAsync.HandleAsync(cmd).ConfigureAwait(false);
            return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<long>(id), data.CorrelationId));
        }

        private static void GuardNpgsqlTransaction(DbTransaction transaction)
        {
            if (transaction is not NpgsqlTransaction)
            {
                throw new InvalidOperationException(
                    $"Expected NpgsqlTransaction but received '{transaction.GetType().FullName}'. " +
                    "The transaction must be opened on an NpgsqlConnection from the Npgsql provider.");
            }
        }
    }
}
