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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Configuration for message history tracking.
    /// </summary>
    public interface IHistoryConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        /// <summary>
        /// If true, message lifecycle events are recorded in a history table.
        /// Default is false (opt-in).
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Number of days to retain history records before purging. Default is 30.
        /// </summary>
        int RetentionDays { get; set; }

        /// <summary>
        /// Maximum length of exception text stored in history records. Default is 4000.
        /// </summary>
        int MaxExceptionLength { get; set; }

        /// <summary>
        /// If true, the serialized message body and headers are stored with the history record.
        /// Increases storage significantly on high-throughput queues. Default is false.
        /// </summary>
        bool StoreBody { get; set; }

        /// <summary>
        /// Track when messages are enqueued. Default is true.
        /// </summary>
        bool TrackEnqueue { get; set; }

        /// <summary>
        /// Track when messages begin processing. Default is true.
        /// </summary>
        bool TrackProcessing { get; set; }

        /// <summary>
        /// Track when messages are committed (success). Default is true.
        /// </summary>
        bool TrackComplete { get; set; }

        /// <summary>
        /// Track when messages fail and move to error status. Default is true.
        /// </summary>
        bool TrackError { get; set; }

        /// <summary>
        /// Track when messages are deleted. Default is true.
        /// </summary>
        bool TrackDelete { get; set; }

        /// <summary>
        /// Track when messages expire. Default is true.
        /// </summary>
        bool TrackExpire { get; set; }

        /// <summary>
        /// Populates this configuration from transport options.
        /// </summary>
        /// <param name="enabled">Whether history is enabled.</param>
        /// <param name="options">The transport history options to copy from.</param>
        void ApplyTransportOptions(bool enabled, IHistoryTransportOptions options);
    }
}
