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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Writes or updates message history records.
    /// </summary>
    public interface IWriteMessageHistory
    {
        /// <summary>
        /// Creates a new history record when a message is enqueued.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        /// <param name="correlationId">The correlation ID, if available.</param>
        /// <param name="route">The message route, if available.</param>
        /// <param name="messageType">The message body type name, if available.</param>
        /// <param name="body">The serialized body bytes, if StoreBody is enabled.</param>
        /// <param name="headers">The serialized headers bytes, if StoreBody is enabled.</param>
        void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers);

        /// <summary>
        /// Updates a history record when a message begins processing.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        void RecordProcessingStart(string queueId);

        /// <summary>
        /// Updates a history record when a message is committed successfully.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        void RecordComplete(string queueId);

        /// <summary>
        /// Updates a history record when a message fails processing.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        /// <param name="exception">The exception text (will be truncated to MaxExceptionLength).</param>
        void RecordError(string queueId, string exception);

        /// <summary>
        /// Updates a history record when a message is rolled back for retry.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        void RecordRollback(string queueId);

        /// <summary>
        /// Updates a history record when a message is deleted.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        void RecordDelete(string queueId);

        /// <summary>
        /// Updates history records when messages are expired by the expiration monitor.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        void RecordExpire(string queueId);
    }
}
