// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
    /// The hearbeat configuration settings
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
        /// Gets or sets the heart beat time. See also <see cref="Interval"/>
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
        /// This is <see cref="Time"/> / <see cref="Interval"/>
        /// </remarks>
        /// <value>
        /// The heart beat check time.
        /// </value>
        TimeSpan CheckTime { get; }

        /// <summary>
        /// Gets or sets the heart beat interval. See also <see cref="Time"/>
        /// </summary>
        /// <remarks>
        /// 
        /// How often the heart beat is updated. Should be at least 2; Default depends on the transport
        /// 
        /// Say the heart beat time is 600 seconds. If the interval is 4, the hearbeat will be updated about every 150 seconds or so.
        /// 
        /// Higher values are somewhat safer, but increase writes to the transport. Lower values are risky - if the system is having trouble updating the heartbeat
        /// It's possible for multiple workers to get the same record. A value of 2 really means that the heartbeat may only make 1 attempt to be set before getting reset
        /// depending on timing.
        /// 
        /// Lower values are really only risky if using aggressive heartbeat windows.  A heart beat time of 4 seconds and an interval of 2 would give a very narrow
        /// window; a transport under heavy load may struggle to keep the heartbeat updated.
        /// 
        /// </remarks>
        /// <value>
        /// The heart beat interval.
        /// </value>
        int Interval { get; set; }
    }
}
