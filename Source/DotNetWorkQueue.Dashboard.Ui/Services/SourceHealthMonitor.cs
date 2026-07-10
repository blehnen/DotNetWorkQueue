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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Background service that polls configured Dashboard API sources for health status.
    /// Caches health state per source in a thread-safe <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// Polls every 30 seconds with a 5-second timeout per source.
    /// </summary>
    public class SourceHealthMonitor : BackgroundService, ISourceHealthMonitor
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

        private readonly IMultiSourceDashboardApiClient _multiSourceClient;
        private readonly ISourceRegistry _sourceRegistry;
        private readonly ILogger<SourceHealthMonitor> _logger;
        private readonly ConcurrentDictionary<string, SourceHealthState> _healthStates = new();

        /// <summary>
        /// Creates a new <see cref="SourceHealthMonitor"/>.
        /// </summary>
        /// <param name="multiSourceClient">The multi-source client for accessing per-source API clients.</param>
        /// <param name="sourceRegistry">The registry of configured API sources.</param>
        /// <param name="logger">The logger for recording state transitions.</param>
        public SourceHealthMonitor(
            IMultiSourceDashboardApiClient multiSourceClient,
            ISourceRegistry sourceRegistry,
            ILogger<SourceHealthMonitor> logger)
        {
            ArgumentNullException.ThrowIfNull(multiSourceClient);
            ArgumentNullException.ThrowIfNull(sourceRegistry);
            ArgumentNullException.ThrowIfNull(logger);

            _multiSourceClient = multiSourceClient;
            _sourceRegistry = sourceRegistry;
            _logger = logger;
        }

        /// <inheritdoc />
        public SourceHealthState GetHealth(string slug)
        {
            return _healthStates.GetValueOrDefault(slug,
                new SourceHealthState { Status = SourceHealthStatus.Unknown });
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, SourceHealthState> GetAllHealth()
        {
            return new Dictionary<string, SourceHealthState>(_healthStates);
        }

        /// <summary>
        /// Polls all configured sources once and updates cached health state.
        /// Exposed as internal for unit testing via <c>[InternalsVisibleTo]</c>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        internal async Task PollAllSourcesAsync(CancellationToken cancellationToken)
        {
            var sources = _sourceRegistry.GetAll();

            foreach (var source in sources)
            {
                var previousState = _healthStates.GetValueOrDefault(source.Slug);
                var previousStatus = previousState?.Status ?? SourceHealthStatus.Unknown;

                SourceHealthState newState;
                try
                {
                    var client = _multiSourceClient.GetClientForSource(source.Slug);

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(PollTimeout);

                    await client.GetSettingsAsync().WaitAsync(timeoutCts.Token).ConfigureAwait(false);

                    newState = new SourceHealthState
                    {
                        Status = SourceHealthStatus.Healthy,
                        LastChecked = DateTimeOffset.UtcNow,
                        ErrorMessage = null
                    };
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Service is shutting down, stop polling
                    return;
                }
                catch (Exception ex)
                {
                    newState = new SourceHealthState
                    {
                        Status = SourceHealthStatus.Unhealthy,
                        LastChecked = DateTimeOffset.UtcNow,
                        ErrorMessage = ex.Message
                    };
                }

                _healthStates[source.Slug] = newState;

                // Log state transitions only
                if (newState.Status != previousStatus)
                {
                    if (newState.Status == SourceHealthStatus.Healthy)
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                            _logger.LogInformation("Source '{SourceName}' is now Healthy", source.Name);
                    }
                    else if (newState.Status == SourceHealthStatus.Unhealthy && _logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("Source '{SourceName}' is now Unhealthy: {ErrorMessage}", source.Name, newState.ErrorMessage);
                }
            }
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial poll immediately on startup
            await PollAllSourcesAsync(stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    await PollAllSourcesAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    return;
                }
            }
        }
    }
}
