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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Background service that periodically prunes stale consumers from the registry.
    /// </summary>
    internal class ConsumerPruningService : BackgroundService
    {
        private readonly IConsumerRegistry _registry;
        private readonly DashboardOptions _options;
        private readonly ILogger<ConsumerPruningService> _logger;

        public ConsumerPruningService(
            IConsumerRegistry registry,
            DashboardOptions options,
            ILogger<ConsumerPruningService> logger)
        {
            _registry = registry;
            _options = options;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.ConsumerHeartbeatIntervalSeconds),
                        stoppingToken).ConfigureAwait(false);

                    var staleThreshold = TimeSpan.FromSeconds(_options.ConsumerStaleThresholdSeconds);
                    var pruned = _registry.PruneStale(staleThreshold);
                    if (pruned > 0)
                    {
                        _logger.LogInformation("Pruned {Count} stale consumer(s)", pruned);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during consumer pruning");
                }
            }
        }
    }
}
