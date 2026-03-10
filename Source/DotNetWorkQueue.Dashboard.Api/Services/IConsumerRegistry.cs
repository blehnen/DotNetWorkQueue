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

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// In-memory registry for tracking active consumers.
    /// </summary>
    public interface IConsumerRegistry
    {
        /// <summary>
        /// Registers a new consumer and returns its unique identifier.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="machineName">The machine name.</param>
        /// <param name="processId">The process ID.</param>
        /// <param name="friendlyName">An optional friendly name.</param>
        /// <returns>The unique consumer identifier.</returns>
        Guid Register(string queueName, string machineName, int processId, string friendlyName);

        /// <summary>
        /// Updates the heartbeat timestamp for a consumer.
        /// </summary>
        /// <param name="consumerId">The consumer identifier.</param>
        /// <returns>True if the consumer was found and updated; false otherwise.</returns>
        bool Heartbeat(Guid consumerId);

        /// <summary>
        /// Removes a consumer from the registry.
        /// </summary>
        /// <param name="consumerId">The consumer identifier.</param>
        /// <returns>True if the consumer was found and removed; false otherwise.</returns>
        bool Unregister(Guid consumerId);

        /// <summary>
        /// Gets all active consumers.
        /// </summary>
        /// <returns>A list of all registered consumers.</returns>
        IReadOnlyList<ConsumerEntry> GetAll();

        /// <summary>
        /// Gets all consumers for a specific queue, matched by queue name and connection string.
        /// </summary>
        /// <param name="queueId">The dashboard queue identifier.</param>
        /// <returns>A list of consumers matched to the specified queue.</returns>
        IReadOnlyList<ConsumerEntry> GetByQueue(Guid queueId);

        /// <summary>
        /// Gets consumer counts per dashboard queue.
        /// </summary>
        /// <returns>A dictionary mapping queue identifiers to their active consumer count.</returns>
        Dictionary<Guid, int> GetCountsByQueue();

        /// <summary>
        /// Removes consumers that have not sent a heartbeat within the stale threshold.
        /// </summary>
        /// <param name="staleThreshold">The time span after which a consumer is considered stale.</param>
        /// <returns>The number of consumers pruned.</returns>
        int PruneStale(TimeSpan staleThreshold);
    }

    /// <summary>
    /// Represents a registered consumer in the in-memory registry.
    /// </summary>
    public class ConsumerEntry
    {
        /// <summary>Gets or sets the unique consumer identifier.</summary>
        public Guid ConsumerId { get; set; }

        /// <summary>Gets or sets the queue name.</summary>
        public string QueueName { get; set; }

        /// <summary>Gets or sets the machine name.</summary>
        public string MachineName { get; set; }

        /// <summary>Gets or sets the process ID.</summary>
        public int ProcessId { get; set; }

        /// <summary>Gets or sets the optional friendly name.</summary>
        public string FriendlyName { get; set; }

        /// <summary>Gets or sets the registration time.</summary>
        public DateTimeOffset RegisteredAt { get; set; }

        /// <summary>Gets or sets the last heartbeat time.</summary>
        public DateTimeOffset LastHeartbeat { get; set; }

        /// <summary>Gets or sets the matched dashboard queue identifier, if any.</summary>
        public Guid? MatchedQueueId { get; set; }
    }
}
