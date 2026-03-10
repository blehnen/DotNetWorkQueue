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
using System.Linq;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// In-memory implementation of <see cref="IConsumerRegistry"/> using a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    internal class ConsumerRegistry : IConsumerRegistry
    {
        private readonly ConcurrentDictionary<Guid, ConsumerEntry> _consumers = new ConcurrentDictionary<Guid, ConsumerEntry>();
        private readonly IDashboardApi _dashboardApi;

        public ConsumerRegistry(IDashboardApi dashboardApi)
        {
            _dashboardApi = dashboardApi;
        }

        /// <inheritdoc />
        public Guid Register(string queueName, string connectionString, string machineName, int processId, string friendlyName)
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var entry = new ConsumerEntry
            {
                ConsumerId = id,
                QueueName = queueName,
                ConnectionString = connectionString,
                MachineName = machineName,
                ProcessId = processId,
                FriendlyName = friendlyName,
                RegisteredAt = now,
                LastHeartbeat = now,
                MatchedQueueId = FindMatchingQueue(queueName, connectionString)
            };

            _consumers.TryAdd(id, entry);
            return id;
        }

        /// <inheritdoc />
        public bool Heartbeat(Guid consumerId)
        {
            if (!_consumers.TryGetValue(consumerId, out var entry))
                return false;

            entry.LastHeartbeat = DateTimeOffset.UtcNow;
            return true;
        }

        /// <inheritdoc />
        public bool Unregister(Guid consumerId)
        {
            return _consumers.TryRemove(consumerId, out _);
        }

        /// <inheritdoc />
        public IReadOnlyList<ConsumerEntry> GetAll()
        {
            return _consumers.Values.ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<ConsumerEntry> GetByQueue(Guid queueId)
        {
            return _consumers.Values
                .Where(c => c.MatchedQueueId == queueId)
                .ToList();
        }

        /// <inheritdoc />
        public Dictionary<Guid, int> GetCountsByQueue()
        {
            return _consumers.Values
                .Where(c => c.MatchedQueueId.HasValue)
                .GroupBy(c => c.MatchedQueueId.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <inheritdoc />
        public int PruneStale(TimeSpan staleThreshold)
        {
            var cutoff = DateTimeOffset.UtcNow - staleThreshold;
            var staleIds = _consumers
                .Where(kvp => kvp.Value.LastHeartbeat < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            var pruned = 0;
            foreach (var id in staleIds)
            {
                if (_consumers.TryRemove(id, out _))
                    pruned++;
            }
            return pruned;
        }

        private Guid? FindMatchingQueue(string queueName, string connectionString)
        {
            foreach (var connection in _dashboardApi.Connections.Values)
            {
                if (!string.Equals(connection.ConnectionString, connectionString, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var queue in connection.Queues)
                {
                    if (string.Equals(queue.QueueName, queueName, StringComparison.OrdinalIgnoreCase))
                        return queue.Id;
                }
            }
            return null;
        }
    }
}
