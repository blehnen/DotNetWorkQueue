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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Redis transport options. Most features are always enabled since Redis doesn't use a schema.
    /// History-related options are persisted to a Redis key.
    /// </summary>
    public class RedisBaseTransportOptions : IBaseTransportOptions
    {
        /// <inheritdoc/>
        public bool EnablePriority => true;
        /// <inheritdoc/>
        public bool EnableStatus => true;
        /// <inheritdoc/>
        public bool EnableHeartBeat => true;
        /// <inheritdoc/>
        public bool EnableDelayedProcessing => true;
        /// <inheritdoc/>
        public bool EnableStatusTable => true;
        /// <inheritdoc/>
        public bool EnableRoute => true;
        /// <inheritdoc/>
        public bool EnableMessageExpiration => true;

        /// <summary>
        /// Gets or sets whether message history tracking is enabled.
        /// </summary>
        public bool EnableHistory { get; set; }

        /// <summary>
        /// History tracking settings.
        /// </summary>
        public HistoryTransportOptions HistoryOptions { get; set; } = new HistoryTransportOptions();

        IHistoryTransportOptions IBaseTransportOptions.HistoryOptions => HistoryOptions;
    }
}
