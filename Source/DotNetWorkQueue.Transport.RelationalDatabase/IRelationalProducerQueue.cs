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
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Capability-cast extension of <see cref="IProducerQueue{TMessage}"/> exposing
    /// caller-supplied-transaction <c>Send</c>/<c>SendAsync</c> overloads for the
    /// transactional outbox pattern. Implemented by SqlServer and PostgreSQL transport
    /// producers; Memory, Redis, LiteDb, and SQLite producers do NOT implement this
    /// interface (capability-cast deliberately fails).
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <remarks>
    /// On the transaction-aware path the producer never commits, rolls back, or disposes the
    /// caller's transaction or its connection. The retry decorator is bypassed (the
    /// caller owns retry policy). See <c>docs/outbox-pattern.md</c> for the full
    /// lifecycle contract (Phase 7).
    /// </remarks>
    public interface IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Sends a single message inside the caller-supplied transaction.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="transaction">Caller-supplied transaction. The producer enlists
        /// its INSERTs on <c>transaction.Connection</c> and never commits/rolls back/disposes
        /// the caller's resources.</param>
        /// <returns>The queue output message describing the send result.</returns>
        IQueueOutputMessage Send(TMessage message, DbTransaction transaction);

        /// <summary>
        /// Sends a single message with additional metadata inside the caller-supplied
        /// transaction.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">Additional message metadata.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>The queue output message describing the send result.</returns>
        IQueueOutputMessage Send(TMessage message, IAdditionalMessageData data, DbTransaction transaction);

        /// <summary>
        /// Async variant of <see cref="Send(TMessage, DbTransaction)"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>A task producing the queue output message describing the send result.</returns>
        Task<IQueueOutputMessage> SendAsync(TMessage message, DbTransaction transaction);

        /// <summary>
        /// Async variant of <see cref="Send(TMessage, IAdditionalMessageData, DbTransaction)"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">Additional message metadata.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>A task producing the queue output message describing the send result.</returns>
        Task<IQueueOutputMessage> SendAsync(TMessage message, IAdditionalMessageData data, DbTransaction transaction);

        /// <summary>
        /// Sends a batch of messages inside the caller-supplied transaction.
        /// </summary>
        /// <param name="messages">The batch of messages.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>The queue output messages describing the send results.</returns>
        /// <remarks>
        /// Batch type is <see cref="List{T}"/> to match the existing
        /// <see cref="IProducerQueue{TMessage}"/> shape; PROJECT.md spec used
        /// <c>IEnumerable</c>, deviation flagged for verifier in PLAN-2.2.
        /// </remarks>
        IQueueOutputMessages Send(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction);

        /// <summary>
        /// Async batch send inside the caller-supplied transaction.
        /// </summary>
        /// <param name="messages">The batch of messages.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <returns>A task producing the queue output messages describing the send results.</returns>
        Task<IQueueOutputMessages> SendAsync(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction);
    }
}
