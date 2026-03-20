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

        // Settings
        public async Task<DashboardSettingsResponse?> GetSettingsAsync()
        {
            return await _http.GetFromJsonAsync<DashboardSettingsResponse>($"{Base}/settings").ConfigureAwait(false);
        }

        // Connections
        public async Task<List<ConnectionResponse>> GetConnectionsAsync()
        {
            return await _http.GetFromJsonAsync<List<ConnectionResponse>>($"{Base}/connections").ConfigureAwait(false)
                   ?? new List<ConnectionResponse>();
        }

        public async Task<List<QueueInfoResponse>> GetQueuesAsync(Guid connectionId)
        {
            return await _http.GetFromJsonAsync<List<QueueInfoResponse>>($"{Base}/connections/{connectionId}/queues").ConfigureAwait(false)
                   ?? new List<QueueInfoResponse>();
        }

        public async Task<List<JobResponse>> GetJobsAsync(Guid connectionId)
        {
            return await _http.GetFromJsonAsync<List<JobResponse>>($"{Base}/connections/{connectionId}/jobs").ConfigureAwait(false)
                   ?? new List<JobResponse>();
        }

        // Queue info
        public async Task<QueueStatusResponse?> GetQueueStatusAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<QueueStatusResponse>($"{Base}/queues/{queueId}/status").ConfigureAwait(false);
        }

        public async Task<QueueFeaturesResponse?> GetQueueFeaturesAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<QueueFeaturesResponse>($"{Base}/queues/{queueId}/features").ConfigureAwait(false);
        }

        public async Task<ConfigurationResponse?> GetQueueConfigurationAsync(Guid queueId)
        {
            return await _http.GetFromJsonAsync<ConfigurationResponse>($"{Base}/queues/{queueId}/configuration").ConfigureAwait(false);
        }

        // Messages
        public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/messages?pageIndex={pageIndex}&pageSize={pageSize}";
            if (status.HasValue)
                url += $"&status={status.Value}";
            return await _http.GetFromJsonAsync<PagedResponse<MessageResponse>>(url).ConfigureAwait(false)
                   ?? new PagedResponse<MessageResponse>();
        }

        public async Task<long> GetMessageCountAsync(Guid queueId, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/messages/count";
            if (status.HasValue)
                url += $"?status={status.Value}";
            return await _http.GetFromJsonAsync<long>(url).ConfigureAwait(false);
        }

        public async Task<MessageResponse?> GetMessageDetailAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageResponse>($"{Base}/queues/{queueId}/messages/{messageId}").ConfigureAwait(false);
        }

        public async Task<MessageBodyResponse?> GetMessageBodyAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageBodyResponse>($"{Base}/queues/{queueId}/messages/{messageId}/body").ConfigureAwait(false);
        }

        public async Task<MessageHeadersResponse?> GetMessageHeadersAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<MessageHeadersResponse>($"{Base}/queues/{queueId}/messages/{messageId}/headers").ConfigureAwait(false);
        }

        public async Task<List<ErrorRetryResponse>> GetMessageRetriesAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<List<ErrorRetryResponse>>($"{Base}/queues/{queueId}/messages/{messageId}/retries").ConfigureAwait(false)
                   ?? new List<ErrorRetryResponse>();
        }

        // Stale messages
        public async Task<PagedResponse<MessageResponse>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds = 60, int pageIndex = 0, int pageSize = 25)
        {
            return await _http.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                       $"{Base}/queues/{queueId}/messages/stale?thresholdSeconds={thresholdSeconds}&pageIndex={pageIndex}&pageSize={pageSize}").ConfigureAwait(false)
                   ?? new PagedResponse<MessageResponse>();
        }

        // Errors
        public async Task<PagedResponse<ErrorMessageResponse>> GetErrorsAsync(Guid queueId, int pageIndex = 0, int pageSize = 25)
        {
            return await _http.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                       $"{Base}/queues/{queueId}/errors?pageIndex={pageIndex}&pageSize={pageSize}").ConfigureAwait(false)
                   ?? new PagedResponse<ErrorMessageResponse>();
        }

        // Write operations
        public async Task<bool> DeleteMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.DeleteAsync($"{Base}/queues/{queueId}/messages/{messageId}").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        public async Task<DeleteAllResponse> DeleteAllErrorsAsync(Guid queueId)
        {
            var response = await _http.DeleteAsync($"{Base}/queues/{queueId}/errors").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DeleteAllResponse>().ConfigureAwait(false)
                   ?? new DeleteAllResponse();
        }

        public async Task<bool> RequeueMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/{messageId}/requeue", null).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/{messageId}/reset", null).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateMessageBodyAsync(Guid queueId, string messageId, EditMessageBodyRequest request)
        {
            var response = await _http.PutAsJsonAsync($"{Base}/queues/{queueId}/messages/{messageId}/body", request).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        public async Task<BulkActionResponse> RequeueAllErrorsAsync(Guid queueId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/errors/requeue-all", null).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BulkActionResponse>().ConfigureAwait(false)
                   ?? new BulkActionResponse();
        }

        public async Task<BulkActionResponse> ResetAllStaleAsync(Guid queueId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/reset-all", null).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BulkActionResponse>().ConfigureAwait(false)
                   ?? new BulkActionResponse();
        }

        // Consumers
        public async Task<List<ConsumerInfoResponse>> GetConsumersAsync(Guid? queueId = null)
        {
            var url = $"{Base}/consumers";
            if (queueId.HasValue)
                url += $"?queueId={queueId.Value}";
            return await _http.GetFromJsonAsync<List<ConsumerInfoResponse>>(url).ConfigureAwait(false)
                   ?? new List<ConsumerInfoResponse>();
        }

        public async Task<Dictionary<Guid, int>> GetConsumerCountsAsync()
        {
            return await _http.GetFromJsonAsync<Dictionary<Guid, int>>($"{Base}/consumers/count").ConfigureAwait(false)
                   ?? new Dictionary<Guid, int>();
        }

        // Cancellation
        public async Task<bool> CancelMessageAsync(Guid queueId, string messageId)
        {
            var response = await _http.PostAsync($"{Base}/queues/{queueId}/messages/{messageId}/cancel", null).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }

        // History
        public async Task<PagedResponse<HistoryResponse>> GetHistoryAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/history?pageIndex={pageIndex}&pageSize={pageSize}";
            if (status.HasValue) url += $"&status={status.Value}";
            return await _http.GetFromJsonAsync<PagedResponse<HistoryResponse>>(url).ConfigureAwait(false)
                   ?? new PagedResponse<HistoryResponse>();
        }

        public async Task<long> GetHistoryCountAsync(Guid queueId, int? status = null)
        {
            var url = $"{Base}/queues/{queueId}/history/count";
            if (status.HasValue) url += $"?status={status.Value}";
            return await _http.GetFromJsonAsync<long>(url).ConfigureAwait(false);
        }

        public async Task<HistoryResponse?> GetHistoryByMessageIdAsync(Guid queueId, string messageId)
        {
            return await _http.GetFromJsonAsync<HistoryResponse>(
                       $"{Base}/queues/{queueId}/history/{messageId}").ConfigureAwait(false);
        }

        public async Task<DeleteAllResponse> PurgeHistoryAsync(Guid queueId, int? olderThanDays = null)
        {
            var url = $"{Base}/queues/{queueId}/history";
            if (olderThanDays.HasValue) url += $"?olderThanDays={olderThanDays.Value}";
            var response = await _http.DeleteAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DeleteAllResponse>().ConfigureAwait(false)
                   ?? new DeleteAllResponse();
        }
    }
}
