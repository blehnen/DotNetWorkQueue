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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite-specific <see cref="RelationalProducerQueue{T}"/> that overrides the
    /// four caller-supplied-transaction hooks to dispatch
    /// <see cref="RelationalSendMessageCommand"/> instances through the registered
    /// SQLite <c>SendMessageCommandHandler</c> / <c>SendMessageCommandHandlerAsync</c>.
    /// Validates the caller's transaction at the producer surface (fail-fast,
    /// boundary-checked) before any handler dispatch.
    /// </summary>
    /// <remarks>
    /// Unlike SqlServer / PostgreSQL counterparts, this class does NOT enforce a
    /// transport-specific transaction type guard (e.g., no <c>GuardSqliteTransaction</c>
    /// check). The SQLite send handlers operate at <see cref="System.Data.IDbConnection"/> /
    /// <see cref="System.Data.IDbTransaction"/> interface level throughout — accepting any
    /// caller-supplied <see cref="DbTransaction"/> whose underlying provider matches the
    /// queue's configured SQLite connection. The validator (
    /// <see cref="ExternalTransactionValidator"/>) ensures the caller's transaction's
    /// database name matches the configured queue database via symmetric path
    /// canonicalization (<see cref="SqLiteExternalDbNameExtractor"/> +
    /// <see cref="DotNetWorkQueue.Transport.SQLite.SqliteNormalizedConnectionInformation"/>).
    /// </remarks>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public sealed class SqLiteRelationalProducerQueue<TMessage>
        : RelationalProducerQueue<TMessage>
        where TMessage : class
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendHandler;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendHandlerAsync;
        private readonly ExternalTransactionValidator _validator;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IMessageFactory _messageFactory;
        private readonly GenerateMessageHeaders _generateMessageHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteRelationalProducerQueue{TMessage}"/> class.
        /// </summary>
        /// <param name="configuration">Producer configuration.</param>
        /// <param name="sendMessages">Send-messages orchestrator (used by the inherited non-transaction path).</param>
        /// <param name="messageFactory">Message factory for the base class (non-transaction path).</param>
        /// <param name="log">Logger.</param>
        /// <param name="generateMessageHeaders">Standard header generator.</param>
        /// <param name="addStandardMessageHeaders">Standard header populator.</param>
        /// <param name="sendHandler">Registered sync handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="sendHandlerAsync">Registered async handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="validator">External-transaction validator (runs at the API boundary).</param>
        /// <param name="sentMessageFactory">Factory for the <see cref="ISentMessage"/> returned to callers.</param>
        /// <param name="ownMessageFactory">Same <see cref="IMessageFactory"/> instance retained for the
        /// caller-transaction path; re-injected because the base type seals its own copy as private.</param>
        public SqLiteRelationalProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log,
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders,
            ICommandHandlerWithOutput<SendMessageCommand, long> sendHandler,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendHandlerAsync,
            ExternalTransactionValidator validator,
            ISentMessageFactory sentMessageFactory,
            IMessageFactory ownMessageFactory)
            : base(configuration, sendMessages, messageFactory, log,
                   generateMessageHeaders, addStandardMessageHeaders)
        {
            Guard.NotNull(() => sendHandler, sendHandler);
            Guard.NotNull(() => sendHandlerAsync, sendHandlerAsync);
            Guard.NotNull(() => validator, validator);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => ownMessageFactory, ownMessageFactory);
            _sendHandler = sendHandler;
            _sendHandlerAsync = sendHandlerAsync;
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
            return SendOne(message, data ?? new AdditionalMessageData(), transaction);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessage> SendWithExternalTransactionAsync(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            return await SendOneAsync(message, data ?? new AdditionalMessageData(), transaction)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);

            var rc = new List<IQueueOutputMessage>(messages.Count);
            foreach (var m in messages)
            {
                try
                {
                    rc.Add(SendOne(m.Message, m.MessageData ?? new AdditionalMessageData(), transaction));
                }
                catch (Exception error)
                {
                    rc.Add(new QueueOutputMessage(
                        _sentMessageFactory.Create(null, (m.MessageData ?? new AdditionalMessageData()).CorrelationId),
                        error));
                }
            }
            return new QueueOutputMessages(rc);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);

            var rc = new List<IQueueOutputMessage>(messages.Count);
            foreach (var m in messages)
            {
                try
                {
                    rc.Add(await SendOneAsync(m.Message,
                        m.MessageData ?? new AdditionalMessageData(), transaction).ConfigureAwait(false));
                }
                catch (Exception error)
                {
                    rc.Add(new QueueOutputMessage(
                        _sentMessageFactory.Create(null, (m.MessageData ?? new AdditionalMessageData()).CorrelationId),
                        error));
                }
            }
            return new QueueOutputMessages(rc);
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
    }
}
