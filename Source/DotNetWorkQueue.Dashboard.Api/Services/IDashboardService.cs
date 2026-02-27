// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Dashboard.Api.Models;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Service layer for dashboard operations. Sits between controllers and the transport query handlers.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>Gets all registered connections.</summary>
        IReadOnlyList<ConnectionResponse> GetConnections();

        /// <summary>Gets all queues for a connection.</summary>
        IReadOnlyList<QueueInfoResponse> GetQueues(Guid connectionId);

        /// <summary>Gets queue status counts.</summary>
        QueueStatusResponse GetStatus(Guid queueId);

        /// <summary>Gets enabled transport features for a queue.</summary>
        QueueFeaturesResponse GetFeatures(Guid queueId);

        /// <summary>Gets a paged list of messages.</summary>
        PagedResponse<MessageResponse> GetMessages(Guid queueId, int pageIndex, int pageSize, int? statusFilter);

        /// <summary>Gets the message count, optionally filtered by status.</summary>
        long GetMessageCount(Guid queueId, int? statusFilter);

        /// <summary>Gets a single message detail.</summary>
        MessageResponse GetMessageDetail(Guid queueId, long messageId);

        /// <summary>Gets messages with stale heartbeats.</summary>
        PagedResponse<MessageResponse> GetStaleMessages(Guid queueId, int thresholdSeconds, int pageIndex, int pageSize);

        /// <summary>Gets a paged list of error messages.</summary>
        PagedResponse<ErrorMessageResponse> GetErrors(Guid queueId, int pageIndex, int pageSize);

        /// <summary>Gets error retry tracking records for a message.</summary>
        IReadOnlyList<ErrorRetryResponse> GetErrorRetries(Guid queueId, long messageId);

        /// <summary>Gets queue configuration as JSON.</summary>
        ConfigurationResponse GetConfiguration(Guid queueId);

        /// <summary>Gets all scheduled jobs for a queue.</summary>
        IReadOnlyList<JobResponse> GetJobs(Guid queueId);

        /// <summary>Gets all scheduled jobs for a connection.</summary>
        IReadOnlyList<JobResponse> GetJobsByConnection(Guid connectionId);
    }
}
