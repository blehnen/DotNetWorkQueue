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

namespace DotNetWorkQueue.Dashboard.Client.Models
{
    // ── Connection models ──

    /// <summary>Represents a registered connection.</summary>
    public class ConnectionResponse
    {
        /// <summary>Gets or sets the connection identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the display name.</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets the queue count.</summary>
        public int QueueCount { get; set; }
    }

    /// <summary>Represents a queue on a connection.</summary>
    public class QueueInfoResponse
    {
        /// <summary>Gets or sets the queue identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the queue name.</summary>
        public string QueueName { get; set; }
    }

    /// <summary>Represents a scheduled job.</summary>
    public class JobResponse
    {
        /// <summary>Gets or sets the job name.</summary>
        public string JobName { get; set; }

        /// <summary>Gets or sets the job event time.</summary>
        public DateTimeOffset? JobEventTime { get; set; }

        /// <summary>Gets or sets the job scheduled time.</summary>
        public DateTimeOffset? JobScheduledTime { get; set; }
    }

    // ── Queue models ──

    /// <summary>Queue message counts by status.</summary>
    public class QueueStatusResponse
    {
        /// <summary>Gets or sets the waiting count.</summary>
        public long Waiting { get; set; }

        /// <summary>Gets or sets the processing count.</summary>
        public long Processing { get; set; }

        /// <summary>Gets or sets the error count.</summary>
        public long Error { get; set; }

        /// <summary>Gets or sets the total count.</summary>
        public long Total { get; set; }
    }

    /// <summary>Queue feature flags.</summary>
    public class QueueFeaturesResponse
    {
        /// <summary>Gets or sets whether priority is enabled.</summary>
        public bool EnablePriority { get; set; }

        /// <summary>Gets or sets whether status is enabled.</summary>
        public bool EnableStatus { get; set; }

        /// <summary>Gets or sets whether the status table is enabled.</summary>
        public bool EnableStatusTable { get; set; }

        /// <summary>Gets or sets whether heartbeat is enabled.</summary>
        public bool EnableHeartBeat { get; set; }

        /// <summary>Gets or sets whether delayed processing is enabled.</summary>
        public bool EnableDelayedProcessing { get; set; }

        /// <summary>Gets or sets whether message expiration is enabled.</summary>
        public bool EnableMessageExpiration { get; set; }

        /// <summary>Gets or sets whether routing is enabled.</summary>
        public bool EnableRoute { get; set; }
    }

    /// <summary>Represents a queued message.</summary>
    public class MessageResponse
    {
        /// <summary>Gets or sets the queue-specific message ID.</summary>
        public string QueueId { get; set; }

        /// <summary>Gets or sets when the message was queued.</summary>
        public DateTimeOffset? QueuedDateTime { get; set; }

        /// <summary>Gets or sets the correlation ID.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Gets or sets the message status.</summary>
        public int? Status { get; set; }

        /// <summary>Gets or sets the message priority.</summary>
        public int? Priority { get; set; }

        /// <summary>Gets or sets the scheduled process time.</summary>
        public DateTimeOffset? QueueProcessTime { get; set; }

        /// <summary>Gets or sets the heartbeat time.</summary>
        public DateTimeOffset? HeartBeat { get; set; }

        /// <summary>Gets or sets the expiration time.</summary>
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>Gets or sets the route.</summary>
        public string Route { get; set; }
    }

    /// <summary>Represents an error message.</summary>
    public class ErrorMessageResponse
    {
        /// <summary>Gets or sets the error tracking ID.</summary>
        public long Id { get; set; }

        /// <summary>Gets or sets the queue-specific message ID.</summary>
        public string QueueId { get; set; }

        /// <summary>Gets or sets the last exception text.</summary>
        public string LastException { get; set; }

        /// <summary>Gets or sets the last exception date.</summary>
        public DateTimeOffset? LastExceptionDate { get; set; }
    }

    /// <summary>Represents an error retry entry.</summary>
    public class ErrorRetryResponse
    {
        /// <summary>Gets or sets the error tracking ID.</summary>
        public long ErrorTrackingId { get; set; }

        /// <summary>Gets or sets the queue-specific message ID.</summary>
        public string QueueId { get; set; }

        /// <summary>Gets or sets the exception type.</summary>
        public string ExceptionType { get; set; }

        /// <summary>Gets or sets the retry count.</summary>
        public int RetryCount { get; set; }
    }

    /// <summary>Represents queue configuration.</summary>
    public class ConfigurationResponse
    {
        /// <summary>Gets or sets the configuration JSON.</summary>
        public string ConfigurationJson { get; set; }
    }

    /// <summary>Represents a message body.</summary>
    public class MessageBodyResponse
    {
        /// <summary>Gets or sets the body content.</summary>
        public string Body { get; set; }

        /// <summary>Gets or sets the body type name.</summary>
        public string TypeName { get; set; }

        /// <summary>Gets or sets whether the body is editable.</summary>
        public bool IsEditable { get; set; }

        /// <summary>Gets or sets whether the message is currently being processed.</summary>
        public bool IsProcessing { get; set; }
    }

    /// <summary>Represents message headers.</summary>
    public class MessageHeadersResponse
    {
        /// <summary>Gets or sets the headers dictionary.</summary>
        public Dictionary<string, string> Headers { get; set; }
    }

    /// <summary>Paginated response wrapper.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>Gets or sets the items.</summary>
        public List<T> Items { get; set; }

        /// <summary>Gets or sets the total count.</summary>
        public long TotalCount { get; set; }

        /// <summary>Gets or sets the page index.</summary>
        public int PageIndex { get; set; }

        /// <summary>Gets or sets the page size.</summary>
        public int PageSize { get; set; }
    }

    /// <summary>Response for bulk actions.</summary>
    public class BulkActionResponse
    {
        /// <summary>Gets or sets the count of affected items.</summary>
        public long Count { get; set; }
    }

    /// <summary>Response for delete all operations.</summary>
    public class DeleteAllResponse
    {
        /// <summary>Gets or sets the number of deleted items.</summary>
        public long Deleted { get; set; }
    }

    /// <summary>Request to edit a message body.</summary>
    public class EditMessageBodyRequest
    {
        /// <summary>Gets or sets the new body content.</summary>
        public string Body { get; set; }
    }

    // ── Consumer models ──

    /// <summary>Response after consumer registration.</summary>
    public class ConsumerRegistrationResponse
    {
        /// <summary>Gets or sets the consumer identifier.</summary>
        public Guid ConsumerId { get; set; }

        /// <summary>Gets or sets the recommended heartbeat interval.</summary>
        public int HeartbeatIntervalSeconds { get; set; }
    }

    /// <summary>Consumer information.</summary>
    public class ConsumerInfoResponse
    {
        /// <summary>Gets or sets the consumer identifier.</summary>
        public Guid ConsumerId { get; set; }

        /// <summary>Gets or sets the queue name.</summary>
        public string QueueName { get; set; }

        /// <summary>Gets or sets the connection string.</summary>
        public string ConnectionString { get; set; }

        /// <summary>Gets or sets the machine name.</summary>
        public string MachineName { get; set; }

        /// <summary>Gets or sets the process ID.</summary>
        public int ProcessId { get; set; }

        /// <summary>Gets or sets the friendly name.</summary>
        public string FriendlyName { get; set; }

        /// <summary>Gets or sets when the consumer registered.</summary>
        public DateTimeOffset RegisteredAt { get; set; }

        /// <summary>Gets or sets the last heartbeat time.</summary>
        public DateTimeOffset LastHeartbeat { get; set; }

        /// <summary>Gets or sets the matched queue identifier.</summary>
        public Guid? MatchedQueueId { get; set; }
    }
}
