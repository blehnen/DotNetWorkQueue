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

namespace DotNetWorkQueue
{
    /// <summary>
    /// History tracking settings persisted with transport options.
    /// </summary>
    public interface IHistoryTransportOptions
    {
        /// <summary>Days to retain history before purging. Default 30.</summary>
        int RetentionDays { get; set; }
        /// <summary>Max exception text length. Default 4000.</summary>
        int MaxExceptionLength { get; set; }
        /// <summary>Store serialized body and headers. Default false.</summary>
        bool StoreBody { get; set; }
        /// <summary>Track enqueue events. Default true.</summary>
        bool TrackEnqueue { get; set; }
        /// <summary>Track processing start. Default true.</summary>
        bool TrackProcessing { get; set; }
        /// <summary>Track successful completion. Default true.</summary>
        bool TrackComplete { get; set; }
        /// <summary>Track errors. Default true.</summary>
        bool TrackError { get; set; }
        /// <summary>Track deletes. Default true.</summary>
        bool TrackDelete { get; set; }
        /// <summary>Track expiration. Default true.</summary>
        bool TrackExpire { get; set; }
        /// <summary>How often the purge monitor runs. Default 1 day.</summary>
        TimeSpan MonitorTime { get; set; }
    }
}
