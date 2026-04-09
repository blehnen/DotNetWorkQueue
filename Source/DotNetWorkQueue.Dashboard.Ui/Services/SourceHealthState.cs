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

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Represents the health status of an API source.
    /// </summary>
    public enum SourceHealthStatus
    {
        /// <summary>
        /// The source has not been polled yet.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The source responded successfully to the last health check.
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// The source failed to respond to the last health check.
        /// </summary>
        Unhealthy = 2
    }

    /// <summary>
    /// Immutable snapshot of a source's health state at a point in time.
    /// </summary>
    public class SourceHealthState
    {
        /// <summary>
        /// The current health status of the source.
        /// </summary>
        public SourceHealthStatus Status { get; init; } = SourceHealthStatus.Unknown;

        /// <summary>
        /// The time of the last health check. <see cref="DateTimeOffset.MinValue"/> if never checked.
        /// </summary>
        public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.MinValue;

        /// <summary>
        /// The error message from the last failed health check, or null if healthy/unknown.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }
}
