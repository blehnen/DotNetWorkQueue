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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Client.Models;

namespace DotNetWorkQueue.Dashboard.Client
{
    /// <summary>
    /// Strongly-typed client for all DotNetWorkQueue Dashboard API endpoints.
    /// </summary>
    public class DashboardApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance using the specified options. Creates and owns an internal <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="options">The client options.</param>
        public DashboardApiClient(DashboardClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.DashboardApiUrl)) throw new ArgumentException("DashboardApiUrl is required.", nameof(options));

            _httpClient = new HttpClient { BaseAddress = new Uri(options.DashboardApiUrl.TrimEnd('/') + "/") };
            if (!string.IsNullOrEmpty(options.ApiKey))
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);

            _ownsHttpClient = true;
        }

        /// <summary>
        /// Initializes a new instance using an externally managed <see cref="HttpClient"/>.
        /// The caller is responsible for configuring BaseAddress and headers.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use.</param>
        public DashboardApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _ownsHttpClient = false;
        }

        /// <summary>
        /// Initializes a new instance using an <see cref="IHttpClientFactory"/>.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="options">The client options.</param>
        public DashboardApiClient(IHttpClientFactory httpClientFactory, DashboardClientOptions options)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.DashboardApiUrl)) throw new ArgumentException("DashboardApiUrl is required.", nameof(options));

            _httpClient = httpClientFactory.CreateClient("DashboardApi");
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(options.DashboardApiUrl.TrimEnd('/') + "/");
                if (!string.IsNullOrEmpty(options.ApiKey))
                    _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }

            _ownsHttpClient = false;
        }

        // ── Connections ──

        /// <summary>Gets all registered connections.</summary>
        public async Task<ApiReturnValue<List<ConnectionResponse>>> GetConnectionsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<ConnectionResponse>>("api/v1/dashboard/connections", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets all queues for a connection.</summary>
        public async Task<ApiReturnValue<List<QueueInfoResponse>>> GetQueuesAsync(Guid connectionId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<QueueInfoResponse>>($"api/v1/dashboard/connections/{connectionId}/queues", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets all scheduled jobs for a connection.</summary>
        public async Task<ApiReturnValue<List<JobResponse>>> GetJobsAsync(Guid connectionId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<JobResponse>>($"api/v1/dashboard/connections/{connectionId}/jobs", cancellationToken).ConfigureAwait(false);
        }

        // ── Queue Status & Features ──

        /// <summary>Gets queue status (message counts by status).</summary>
        public async Task<ApiReturnValue<QueueStatusResponse>> GetQueueStatusAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<QueueStatusResponse>($"api/v1/dashboard/queues/{queueId}/status", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets queue feature flags.</summary>
        public async Task<ApiReturnValue<QueueFeaturesResponse>> GetQueueFeaturesAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<QueueFeaturesResponse>($"api/v1/dashboard/queues/{queueId}/features", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets queue configuration.</summary>
        public async Task<ApiReturnValue<ConfigurationResponse>> GetQueueConfigurationAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<ConfigurationResponse>($"api/v1/dashboard/queues/{queueId}/configuration", cancellationToken).ConfigureAwait(false);
        }

        // ── Messages ──

        /// <summary>Gets paged messages for a queue.</summary>
        public async Task<ApiReturnValue<PagedResponse<MessageResponse>>> GetMessagesAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, int? status = null, CancellationToken cancellationToken = default)
        {
            var url = $"api/v1/dashboard/queues/{queueId}/messages?pageIndex={pageIndex}&pageSize={pageSize}";
            if (status.HasValue) url += $"&status={status.Value}";
            return await GetAsync<PagedResponse<MessageResponse>>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets the message count for a queue.</summary>
        public async Task<ApiReturnValue<long>> GetMessageCountAsync(Guid queueId, int? status = null, CancellationToken cancellationToken = default)
        {
            var url = $"api/v1/dashboard/queues/{queueId}/messages/count";
            if (status.HasValue) url += $"?status={status.Value}";
            return await GetAsync<long>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets a specific message by ID.</summary>
        public async Task<ApiReturnValue<MessageResponse>> GetMessageAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<MessageResponse>($"api/v1/dashboard/queues/{queueId}/messages/{messageId}", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets stale messages for a queue.</summary>
        public async Task<ApiReturnValue<PagedResponse<MessageResponse>>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds = 60, int pageIndex = 0, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            return await GetAsync<PagedResponse<MessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/messages/stale?thresholdSeconds={thresholdSeconds}&pageIndex={pageIndex}&pageSize={pageSize}",
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets the body of a message.</summary>
        public async Task<ApiReturnValue<MessageBodyResponse>> GetMessageBodyAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<MessageBodyResponse>($"api/v1/dashboard/queues/{queueId}/messages/{messageId}/body", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets the headers of a message.</summary>
        public async Task<ApiReturnValue<MessageHeadersResponse>> GetMessageHeadersAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<MessageHeadersResponse>($"api/v1/dashboard/queues/{queueId}/messages/{messageId}/headers", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deletes a specific message.</summary>
        public async Task<ApiReturnValue<bool>> DeleteMessageAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await SendAsync(HttpMethod.Delete, $"api/v1/dashboard/queues/{queueId}/messages/{messageId}", null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Edits the body of a message.</summary>
        public async Task<ApiReturnValue<bool>> EditMessageBodyAsync(Guid queueId, string messageId, string body, CancellationToken cancellationToken = default)
        {
            var request = new EditMessageBodyRequest { Body = body };
            return await SendAsync(HttpMethod.Put, $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/body", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Resets a stale message for reprocessing.</summary>
        public async Task<ApiReturnValue<bool>> ResetMessageAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await SendAsync(HttpMethod.Post, $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/reset", null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Resets all stale messages for a queue.</summary>
        public async Task<ApiReturnValue<BulkActionResponse>> ResetAllStaleMessagesAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await SendWithResponseAsync<BulkActionResponse>(HttpMethod.Post, $"api/v1/dashboard/queues/{queueId}/messages/reset-all", null, cancellationToken).ConfigureAwait(false);
        }

        // ── Errors ──

        /// <summary>Gets paged errors for a queue.</summary>
        public async Task<ApiReturnValue<PagedResponse<ErrorMessageResponse>>> GetErrorsAsync(Guid queueId, int pageIndex = 0, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            return await GetAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{queueId}/errors?pageIndex={pageIndex}&pageSize={pageSize}",
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets error retries for a specific message.</summary>
        public async Task<ApiReturnValue<List<ErrorRetryResponse>>> GetErrorRetriesAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<ErrorRetryResponse>>($"api/v1/dashboard/queues/{queueId}/messages/{messageId}/retries", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Requeues an error message for reprocessing.</summary>
        public async Task<ApiReturnValue<bool>> RequeueErrorMessageAsync(Guid queueId, string messageId, CancellationToken cancellationToken = default)
        {
            return await SendAsync(HttpMethod.Post, $"api/v1/dashboard/queues/{queueId}/messages/{messageId}/requeue", null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Requeues all error messages for a queue.</summary>
        public async Task<ApiReturnValue<BulkActionResponse>> RequeueAllErrorsAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await SendWithResponseAsync<BulkActionResponse>(HttpMethod.Post, $"api/v1/dashboard/queues/{queueId}/errors/requeue-all", null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deletes all errors for a queue.</summary>
        public async Task<ApiReturnValue<DeleteAllResponse>> DeleteAllErrorsAsync(Guid queueId, CancellationToken cancellationToken = default)
        {
            return await SendWithResponseAsync<DeleteAllResponse>(HttpMethod.Delete, $"api/v1/dashboard/queues/{queueId}/errors", null, cancellationToken).ConfigureAwait(false);
        }

        // ── Consumers ──

        /// <summary>Gets active consumers, optionally filtered by queue.</summary>
        public async Task<ApiReturnValue<List<ConsumerInfoResponse>>> GetConsumersAsync(Guid? queueId = null, CancellationToken cancellationToken = default)
        {
            var url = "api/v1/dashboard/consumers";
            if (queueId.HasValue) url += $"?queueId={queueId.Value}";
            return await GetAsync<List<ConsumerInfoResponse>>(url, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets consumer counts per dashboard queue.</summary>
        public async Task<ApiReturnValue<Dictionary<Guid, int>>> GetConsumerCountsAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<Dictionary<Guid, int>>("api/v1/dashboard/consumers/count", cancellationToken).ConfigureAwait(false);
        }

        // ── HTTP helpers ──

        private async Task<ApiReturnValue<T>> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            using (var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ApiReturnValue<T>.Fail(response.StatusCode, errorBody);
                }

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return ApiReturnValue<T>.Ok(response.StatusCode, value);
            }
        }

        private async Task<ApiReturnValue<bool>> SendAsync(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(method, url))
            {
                if (body != null)
                    request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return ApiReturnValue<bool>.Fail(response.StatusCode, errorBody);
                    }

                    return ApiReturnValue<bool>.Ok(response.StatusCode, true);
                }
            }
        }

        private async Task<ApiReturnValue<T>> SendWithResponseAsync<T>(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(method, url))
            {
                if (body != null)
                    request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return ApiReturnValue<T>.Fail(response.StatusCode, errorBody);
                    }

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
                    return ApiReturnValue<T>.Ok(response.StatusCode, value);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_ownsHttpClient)
                _httpClient.Dispose();
        }
    }
}
