// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
    /// Defines the configuration settings for a task scheduler
    /// </summary>
    public interface ITaskSchedulerConfiguration : IReadonly, ISetReadonly
    {
        /// <summary>
        /// The maximum amount of threads to use to process messages
        /// </summary>
        /// <value>
        /// The maximum threads.
        /// </value>
        int MaximumThreads { get; set; }

        /// <summary>
        /// The minimum amount of threads to keep alive. If no work is present, and a thread has reached <see cref="ThreadIdleTimeout"/> the thread may be removed 
        /// from the thread pool. It will be re-created as needed.
        /// </summary>
        /// <value>
        /// The minimum threads.
        /// </value>
        int MinimumThreads { get; set; }

        /// <summary>
        /// The maximum amount of items to hold in memory, after the threads are all in a work state. 
        /// </summary>
        /// <remarks>
        /// A large setting has drawbacks, depending on the transport being used.  
        /// 
        /// If the SQL server transport is being used, items will be de-queued
        /// and will be stored in the in memory queue. You should be aware of the following:
        /// 
        /// 1) Items contained in the in memory queue will move to a worker thread when a free worker thread is present
        /// 2) If the heartbeat is enabled, items in the memory queue will send heartbeats.
        /// 3) A large queue may affect your load balancing. If another machine/process has free slots, but another queue is holding onto work
        /// in the in memory queue, those items will not be available 
        /// 
        /// A work stealing queue that can communicate between machines/processes could handle #3. There is no built in implementation of this.
        /// 
        /// </remarks>
        /// <value>
        /// The maximum size of the queue.
        /// </value>
        int MaxQueueSize { get; set; }

        /// <summary>
        /// If a worker thread has been idle for this amount of time and the current thread count is greater than <see cref="MinimumThreads"/>
        /// the thread will be removed from the thread pool.
        /// </summary>
        /// <value>
        /// The thread idle timeout.
        /// </value>
        TimeSpan ThreadIdleTimeout { get; set; }

        /// <summary>
        /// How long to wait for thread pool threads to exit when shutting down
        /// </summary>
        TimeSpan WaitForTheadPoolToFinish { get; set; }
    }
}
