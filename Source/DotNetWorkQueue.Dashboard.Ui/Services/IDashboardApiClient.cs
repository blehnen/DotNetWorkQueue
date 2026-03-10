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
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.Models;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    public interface IDashboardApiClient
    {
        // Connections
        Task<List<ConnectionResponse>> GetConnectionsAsync();
        Task<List<QueueInfoResponse>> GetQueuesAsync(Guid connectionId);
        Task<List<JobResponse>> GetJobsAsync(Guid connectionId);

        // Queue info
        Task<QueueStatusResponse?> GetQueueStatusAsync(Guid queueId);
        Task<QueueFeaturesResponse?> GetQueueFeaturesAsync(Guid queueId);
        Task<ConfigurationResponse?> GetQueueConfigurationAsync(Guid queueId);

        // Messages
        Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, int? status = null);
        Task<long> GetMessageCountAsync(Guid queueId, int? status = null);
        Task<MessageResponse?> GetMessageDetailAsync(Guid queueId, string messageId);
        Task<MessageBodyResponse?> GetMessageBodyAsync(Guid queueId, string messageId);
        Task<MessageHeadersResponse?> GetMessageHeadersAsync(Guid queueId, string messageId);
        Task<List<ErrorRetryResponse>> GetMessageRetriesAsync(Guid queueId, string messageId);

        // Stale messages
        Task<PagedResponse<MessageResponse>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds = 60, int pageIndex = 0, int pageSize = 25);

        // Errors
        Task<PagedResponse<ErrorMessageResponse>> GetErrorsAsync(Guid queueId, int pageIndex = 0, int pageSize = 25);

        // Write operations
        Task<bool> DeleteMessageAsync(Guid queueId, string messageId);
        Task<DeleteAllResponse> DeleteAllErrorsAsync(Guid queueId);
        Task<bool> RequeueMessageAsync(Guid queueId, string messageId);
        Task<bool> ResetMessageAsync(Guid queueId, string messageId);
        Task<bool> UpdateMessageBodyAsync(Guid queueId, string messageId, EditMessageBodyRequest request);
        Task<BulkActionResponse> RequeueAllErrorsAsync(Guid queueId);
        Task<BulkActionResponse> ResetAllStaleAsync(Guid queueId);

        // Consumers
        Task<List<ConsumerInfoResponse>> GetConsumersAsync(Guid? queueId = null);
        Task<Dictionary<Guid, int>> GetConsumerCountsAsync();
    }
}
