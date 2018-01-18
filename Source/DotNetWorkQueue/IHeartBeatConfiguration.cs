// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
    /// The heartbeat configuration settings
    /// <remarks>
    /// The queue can 'ping' a record that is being processed and keep it alive. This allows for automatic recovery of records in which the processor
    /// has died and records are stuck in a processing state.
    /// </remarks>
    /// </summary>
    public interface IHeartBeatConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        /// <summary>
        /// Configuration settings for the heart beat thread pool
        /// </summary>
        /// <value>
        /// The thread pool configuration.
        /// </value>
        IHeartBeatThreadPoolConfiguration ThreadPoolConfiguration { get; }

        /// <summary>
        /// Gets a value indicating whether the queue supports a heart beat
        /// </summary>
        /// <remarks>The transport determines if this is supported or not.</remarks>
        /// <value>
        ///   <c>true</c> if [heart beat enabled]; otherwise, <c>false</c>.
        /// </value>
        bool Enabled { get; }

        /// <summary>
        /// Gets or sets the heart beat time. \
        /// </summary>
        /// <remarks>This controls how long before a record is considered 'dead' because the heartbeat is out side of this window. The status will be reset, allowing re-processing</remarks>
        /// <value>
        /// The heart beat time
        /// </value>
        TimeSpan Time { get; set; }

        /// <summary>
        /// How often the heartbeat will be updated.
        /// </summary>
        /// <remarks>
        /// This is expected to be in schyntax format - https://github.com/schyntax/cs-schyntax
        /// </remarks>
        string UpdateTime { get; set; }
    }
}
