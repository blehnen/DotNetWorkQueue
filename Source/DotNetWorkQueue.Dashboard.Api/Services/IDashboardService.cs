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
using System.Threading.Tasks;
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

        /// <summary>Gets enabled transport features for a queue.</summary>
        QueueFeaturesResponse GetFeatures(Guid queueId);

        /// <summary>Gets queue status counts.</summary>
        Task<QueueStatusResponse> GetStatusAsync(Guid queueId);

        /// <summary>Gets a paged list of messages.</summary>
        Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid queueId, int pageIndex, int pageSize, int? statusFilter);

        /// <summary>Gets the message count, optionally filtered by status.</summary>
        Task<long> GetMessageCountAsync(Guid queueId, int? statusFilter);

        /// <summary>Gets a single message detail.</summary>
        Task<MessageResponse> GetMessageDetailAsync(Guid queueId, long messageId);

        /// <summary>Gets messages with stale heartbeats.</summary>
        Task<PagedResponse<MessageResponse>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds, int pageIndex, int pageSize);

        /// <summary>Gets a paged list of error messages.</summary>
        Task<PagedResponse<ErrorMessageResponse>> GetErrorsAsync(Guid queueId, int pageIndex, int pageSize);

        /// <summary>Gets error retry tracking records for a message.</summary>
        Task<IReadOnlyList<ErrorRetryResponse>> GetErrorRetriesAsync(Guid queueId, long messageId);

        /// <summary>Gets queue configuration as JSON.</summary>
        Task<ConfigurationResponse> GetConfigurationAsync(Guid queueId);

        /// <summary>Gets all scheduled jobs for a queue.</summary>
        Task<IReadOnlyList<JobResponse>> GetJobsAsync(Guid queueId);

        /// <summary>Gets all scheduled jobs for a connection.</summary>
        Task<IReadOnlyList<JobResponse>> GetJobsByConnectionAsync(Guid connectionId);
    }
}
