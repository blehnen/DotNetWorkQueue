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
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Relational-transport variant of <see cref="ProducerQueue{T}"/> exposing
    /// caller-supplied-transaction <c>Send</c>/<c>SendAsync</c> overloads. The four
    /// transaction-aware virtual methods throw <see cref="InvalidOperationException"/> by default;
    /// SqlServer and PostgreSQL producers (Phases 3 and 4) override them to invoke the
    /// per-transport <c>SendMessageCommandHandler</c> directly with the caller's
    /// transaction in scope.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelationalProducerQueue{T}"/>
        /// class. Constructor parameters mirror the base
        /// <see cref="ProducerQueue{T}"/>; SimpleInjector resolves them per the existing
        /// transport DI conventions.
        /// </summary>
        /// <param name="configuration">The queue producer configuration.</param>
        /// <param name="sendMessages">The send-messages service.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="log">The logger.</param>
        /// <param name="generateMessageHeaders">The generate-message-headers delegate.</param>
        /// <param name="addStandardMessageHeaders">The add-standard-message-headers delegate.</param>
        public RelationalProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log,
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders)
            : base(configuration, sendMessages, messageFactory, log, generateMessageHeaders, addStandardMessageHeaders)
        {
        }

        // --- IRelationalProducerQueue<T> public overloads ---

        /// <inheritdoc />
        public IQueueOutputMessage Send(T message, DbTransaction transaction)
            => SendWithExternalTransaction(message, null, transaction);

        /// <inheritdoc />
        public IQueueOutputMessage Send(T message, IAdditionalMessageData data, DbTransaction transaction)
            => SendWithExternalTransaction(message, data, transaction);

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
            => SendWithExternalTransactionBatch(messages, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessage> SendAsync(T message, DbTransaction transaction)
            => SendWithExternalTransactionAsync(message, null, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessage> SendAsync(T message, IAdditionalMessageData data, DbTransaction transaction)
            => SendWithExternalTransactionAsync(message, data, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessages> SendAsync(List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
            => SendWithExternalTransactionBatchAsync(messages, transaction);

        // --- 4 hooks (Phase 3/4 override these) ---

        /// <summary>
        /// Synchronous single-message caller-transaction send. Phase 3 (SqlServer) and Phase 4
        /// (PostgreSQL) override this to enlist the queue INSERTs on the caller's
        /// transaction. Default implementation throws.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">Optional additional message data; <c>null</c> when invoked
        /// from the no-data overload.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>The queue output message describing the send result.</returns>
        /// <exception cref="InvalidOperationException">Thrown by the base implementation
        /// when no transport-specific override is registered.</exception>
        protected virtual IQueueOutputMessage SendWithExternalTransaction(T message,
            IAdditionalMessageData data, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>
        /// Async equivalent of <see cref="SendWithExternalTransaction"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">Optional additional message data; <c>null</c> when invoked
        /// from the no-data overload.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>A task producing the queue output message describing the send result.</returns>
        /// <exception cref="InvalidOperationException">Thrown by the base implementation
        /// when no transport-specific override is registered.</exception>
        protected virtual Task<IQueueOutputMessage> SendWithExternalTransactionAsync(T message,
            IAdditionalMessageData data, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>
        /// Synchronous batch caller-transaction send.
        /// </summary>
        /// <param name="messages">The batch of messages.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>The queue output messages describing the send results.</returns>
        /// <exception cref="InvalidOperationException">Thrown by the base implementation
        /// when no transport-specific override is registered.</exception>
        protected virtual IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>
        /// Async batch caller-transaction send.
        /// </summary>
        /// <param name="messages">The batch of messages.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>A task producing the queue output messages describing the send results.</returns>
        /// <exception cref="InvalidOperationException">Thrown by the base implementation
        /// when no transport-specific override is registered.</exception>
        protected virtual Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(
            List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        private static string NotConfiguredMessage()
            => "Caller-supplied-transaction send is not implemented for this transport. " +
               "Override SendWithExternalTransaction (and the batch + async variants) " +
               "in a transport-specific subclass, or resolve a SqlServer/PostgreSQL " +
               "producer that already does.";
    }
}
