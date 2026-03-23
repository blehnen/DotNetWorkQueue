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

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Default implementation of <see cref="IHistoryTransportOptions"/> with sensible defaults.
    /// Serialized as part of transport options.
    /// </summary>
    public class HistoryTransportOptions : IHistoryTransportOptions
    {
        /// <inheritdoc />
        public int RetentionDays { get; set; } = 30;
        /// <inheritdoc />
        public int MaxExceptionLength { get; set; } = 4000;
        /// <inheritdoc />
        public bool StoreBody { get; set; }
        /// <inheritdoc />
        public bool TrackEnqueue { get; set; } = true;
        /// <inheritdoc />
        public bool TrackProcessing { get; set; } = true;
        /// <inheritdoc />
        public bool TrackComplete { get; set; } = true;
        /// <inheritdoc />
        public bool TrackError { get; set; } = true;
        /// <inheritdoc />
        public bool TrackDelete { get; set; } = true;
        /// <inheritdoc />
        public bool TrackExpire { get; set; } = true;
        /// <inheritdoc />
        public TimeSpan MonitorTime { get; set; } = TimeSpan.FromDays(1);
    }
}
