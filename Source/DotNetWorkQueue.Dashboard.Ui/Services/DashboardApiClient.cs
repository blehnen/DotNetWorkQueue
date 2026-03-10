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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.Models;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    public class DashboardApiClient : IDashboardApiClient
    {
        private readonly HttpClient _http;
        private const string Base = "api/v1/dashboard";

        public DashboardApiClient(HttpClient http)
        {
            _http = http;
        }

        // Connections
        public async Task<List<ConnectionResponse>> GetConnectionsAsync()
        {
            return await _http.GetFromJsonAsync<List<ConnectionResponse>>($"{Base}/connections")
                   ?? new List<ConnectionResponse>();
        }

        public async Task<List<QueueInfoResponse>> GetQueuesAsync(Guid connectionId)
        {
            return await _http.GetFromJsonAsync<List<QueueInfoResponse>>($"{Base}/connections/{connectionId}/queues")
                   ?? new List<QueueInfoResponse>();
        }

        public async Task<List<JobResponse>> GetJobsAsync(Guid connectionId)
        {
            return await _http.GetFromJsonAsync<List<JobResponse>>($"{Base}/connections/{connectionId}/jobs")
                   ?? new List<JobResponse>();
        }

        // Queue info
        public async Task<QueueStatusResponse?> GetQueueStatusAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<QueueStatusResponse>($"{Base}/queues/{queueId}/status");
        }

        public async Task<QueueFeaturesResponse?> GetQueueFeaturesAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<QueueFeaturesResponse>($"{Base}/queues/{queueId}/features");
        }

        public async Task<ConfigurationResponse?> GetQueueConfigurationAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<ConfigurationResponse>($"{Base}/queues/{queueId}/configuration");
        }

        // Messages
        public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/messages?pageIndex={pageIndex}&pageSize={pageSize}";
            if (status.HasValue)
                url += $"&status={status.Value}";
            return await _http.GetFromJsonAsync<PagedResponse<MessageResponse>>(url)
                   ?? new PagedResponse<MessageResponse>();
        }

        public async Task<long> GetMessageCountAsync(Guid queueId, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/messages/count";
            if (status.HasValue)
                url += $"?status={status.Value}";
            return await _http.GetFromJsonAsync<long>(url);
        }

        public async Task<MessageResponse?> GetMessageDetailAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageResponse>($"{Base}/queues/{queueId}/messages/{messageId}");
        }

        public async Task<MessageBodyResponse?> GetMessageBodyAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageBodyResponse>($"{Base}/queues/{queueId}/messages/{messageId}/body");
        }

        public async Task<MessageHeadersResponse?> GetMessageHeadersAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageHeadersResponse>($"{Base}/queues/{queueId}/messages/{messageId}/headers");
        }

        public async Task<List<ErrorRetryResponse>> GetMessageRetriesAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<List<ErrorRetryResponse>>($"{Base}/queues/{queueId}/messages/{messageId}/retries")
                   ?? new List<ErrorRetryResponse>();
        }

        // Stale messages
        public async Task<PagedResponse<MessageResponse>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds = 60, int pageIndex = 0, int pageSize = 25)
        {
            return await _http.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                       $"{Base}/queues/{queueId}/messages/stale?thresholdSeconds={thresholdSeconds}&pageIndex={pageIndex}&pageSize={pageSize}")
                   ?? new PagedResponse<MessageResponse>();
        }

        // Errors
        public async Task<PagedResponse<ErrorMessageResponse>> GetErrorsAsync(Guid queueId, int pageIndex = 0, int pageSize = 25)
        {
            return await _http.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                       $"{Base}/queues/{queueId}/errors?pageIndex={pageIndex}&pageSize={pageSize}")
                   ?? new PagedResponse<ErrorMessageResponse>();
        }

        // Write operations
        public async Task<bool> DeleteMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.DeleteAsync($"{Base}/queues/{queueId}/messages/{messageId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<DeleteAllResponse> DeleteAllErrorsAsync(Guid queueId)
        {
            var response = await _http.DeleteAsync($"{Base}/queues/{queueId}/errors");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DeleteAllResponse>()
                   ?? new DeleteAllResponse();
        }

        public async Task<bool> RequeueMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/{messageId}/requeue", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/{messageId}/reset", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateMessageBodyAsync(Guid queueId, string messageId, EditMessageBodyRequest request)
        {
            var response = await _http.PutAsJsonAsync($"{Base}/queues/{queueId}/messages/{messageId}/body", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<BulkActionResponse> RequeueAllErrorsAsync(Guid queueId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/errors/requeue-all", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BulkActionResponse>()
                   ?? new BulkActionResponse();
        }

        public async Task<BulkActionResponse> ResetAllStaleAsync(Guid queueId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/reset-all", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BulkActionResponse>()
                   ?? new BulkActionResponse();
        }

        // Consumers
        public async Task<List<ConsumerInfoResponse>> GetConsumersAsync(Guid? queueId = null)
        {
            var url = $"{Base}/consumers";
            if (queueId.HasValue)
                url += $"?queueId={queueId.Value}";
            return await _http.GetFromJsonAsync<List<ConsumerInfoResponse>>(url)
                   ?? new List<ConsumerInfoResponse>();
        }

        public async Task<Dictionary<Guid, int>> GetConsumerCountsAsync()
        {
            return await _http.GetFromJsonAsync<Dictionary<Guid, int>>($"{Base}/consumers/count")
                   ?? new Dictionary<Guid, int>();
        }
    }
}
