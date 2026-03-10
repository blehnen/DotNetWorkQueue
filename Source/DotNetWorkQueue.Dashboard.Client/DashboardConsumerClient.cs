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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Dashboard.Client
{
    /// <summary>
    /// Client that automatically registers a consumer with the Dashboard API,
    /// sends periodic heartbeats, and unregisters on disposal.
    /// </summary>
    public class DashboardConsumerClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly DashboardClientOptions _options;
        private readonly Timer _heartbeatTimer;
        private Guid? _consumerId;
        private int _disposed;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Gets the consumer identifier assigned by the Dashboard API after registration.
        /// Null if not yet registered.
        /// </summary>
        public Guid? ConsumerId => _consumerId;

        /// <summary>
        /// Gets whether the client is currently registered with the dashboard.
        /// </summary>
        public bool IsRegistered => _consumerId.HasValue;

        /// <summary>
        /// Initializes a new instance using the specified options.
        /// </summary>
        /// <param name="options">The client options including queue name and connection string.</param>
        public DashboardConsumerClient(DashboardClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.DashboardApiUrl)) throw new ArgumentException("DashboardApiUrl is required.", nameof(options));
            if (string.IsNullOrEmpty(options.QueueName)) throw new ArgumentException("QueueName is required for consumer registration.", nameof(options));
            if (string.IsNullOrEmpty(options.ConnectionString)) throw new ArgumentException("ConnectionString is required for consumer registration.", nameof(options));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.DashboardApiUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(5)
            };
            if (!string.IsNullOrEmpty(options.ApiKey))
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);

            _ownsHttpClient = true;
            _heartbeatTimer = new Timer(HeartbeatCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Initializes a new instance using an externally managed <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The HTTP client (caller manages lifetime and must configure BaseAddress).</param>
        /// <param name="options">The client options including queue name and connection string.</param>
        public DashboardConsumerClient(HttpClient httpClient, DashboardClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.QueueName)) throw new ArgumentException("QueueName is required for consumer registration.", nameof(options));
            if (string.IsNullOrEmpty(options.ConnectionString)) throw new ArgumentException("ConnectionString is required for consumer registration.", nameof(options));

            _ownsHttpClient = false;
            _heartbeatTimer = new Timer(HeartbeatCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Registers this consumer with the Dashboard API and starts the heartbeat timer.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when registration is successful.</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_consumerId.HasValue)
                return;

            var request = new
            {
                QueueName = _options.QueueName,
                ConnectionString = _options.ConnectionString,
                MachineName = Environment.MachineName,
#if NETFULL || NETSTANDARD2_0
                ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
#else
                ProcessId = Environment.ProcessId,
#endif
                FriendlyName = _options.FriendlyName
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var response = await _httpClient.PostAsync("api/v1/dashboard/consumers/register", content, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var registration = JsonSerializer.Deserialize<RegistrationResult>(responseJson, JsonOptions);

                _consumerId = registration.ConsumerId;
                var intervalMs = (registration.HeartbeatIntervalSeconds > 0 ? registration.HeartbeatIntervalSeconds : 30) * 1000;
                _heartbeatTimer.Change(intervalMs, intervalMs);
            }
        }

        /// <summary>
        /// Stops the heartbeat timer and unregisters from the Dashboard API.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when unregistration is done (best-effort).</returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (!_consumerId.HasValue)
                return;

            var id = _consumerId.Value;
            _consumerId = null;

            try
            {
                using (var response = await _httpClient.DeleteAsync($"api/v1/dashboard/consumers/{id}", cancellationToken).ConfigureAwait(false))
                {
                    // Best-effort; ignore failures
                }
            }
            catch
            {
                // Best-effort unregister — swallow exceptions
            }
        }

        private async void HeartbeatCallback(object state)
        {
            if (!_consumerId.HasValue || Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
                return;

            try
            {
                var request = new { ConsumerId = _consumerId.Value };
                var json = JsonSerializer.Serialize(request, JsonOptions);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await _httpClient.PostAsync("api/v1/dashboard/consumers/heartbeat", content).ConfigureAwait(false))
                {
                    if ((int)response.StatusCode == 404)
                    {
                        // Consumer was pruned or unknown; clear registration
                        _consumerId = null;
                        _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            catch
            {
                // Heartbeat failures are non-fatal; the server will prune if enough are missed
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _heartbeatTimer.Dispose();

            // Best-effort synchronous unregister
            if (_consumerId.HasValue)
            {
                try
                {
                    _httpClient.DeleteAsync($"api/v1/dashboard/consumers/{_consumerId.Value}")
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch
                {
                    // Best-effort
                }
                _consumerId = null;
            }

            if (_ownsHttpClient)
                _httpClient.Dispose();
        }

        private class RegistrationResult
        {
            public Guid ConsumerId { get; set; }
            public int HeartbeatIntervalSeconds { get; set; }
        }
    }
}
