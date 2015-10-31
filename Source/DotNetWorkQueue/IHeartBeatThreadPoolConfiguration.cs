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
    /// Heart beat thread pool configuration module
    /// </summary>
    public interface IHeartBeatThreadPoolConfiguration : IReadonly, ISetReadonly
    {
        /// <summary>
        /// How many threads will be used to update the heartbeats.
        /// </summary>
        /// <remarks>
        /// The thread pool is used to notify the worker that a heart beat needs updating. However, a dedicated thread pool is used for
        /// the actual updating, to reduce how long we require the usage of threads from the built in thread pool.
        /// </remarks>
        /// <value>
        /// The max heart beat threads.
        /// </value>
        int ThreadsMax { get; set; }

        /// <summary>
        /// The minimum amount of threads in the thread pool for updating the heartbeat
        /// </summary>
        /// <remarks>
        /// The thread pool is used to notify the worker that a heart beat needs updating. However, a dedicated thread pool is used for
        /// the actual updating, to reduce how long we require the usage of threads from the built in thread pool.
        /// </remarks>
        /// <value>
        /// The min heart beat threads.
        /// </value>
        int ThreadsMin { get; set; }

        /// <summary>
        /// How long before a heart beat worker thread idle time out due to inactivity.
        /// </summary>
        /// <remarks>This only applies if the concurrent thread count is greater than the min</remarks>
        /// <value>
        /// The heart beat thread idle timeout.
        /// </value>
        TimeSpan ThreadIdleTimeout { get; set; }
    }
}
