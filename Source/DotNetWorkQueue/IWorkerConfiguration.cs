// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    /// Defines the configuration settings for a worker.
    /// </summary>
    public interface IWorkerConfiguration: IReadonly, ISetReadonly
    {
        /// <summary>
        /// Gets or sets the worker count.
        /// </summary>
        /// <value>
        /// The worker count.
        /// </value>
        /// <remarks>
        /// This controls how many worker threads are used.
        /// For Sync queues: This is the number of processing threads. Each processing thread dequeues it's own work.
        /// For Async queues: This is the number of dequeue threads. A dequeue thread passes work to a <seealso cref="ITaskFactory"/> for processing.
        /// </remarks>
        int WorkerCount { get; set; }

        /// <summary>
        /// How long to wait for the workers to respond to a cancel request.
        /// </summary>
        /// <remarks>
        /// If thread aborting is disabled, this setting has no affect; we will wait forever for threads to finish working
        /// Otherwise, the thread will be aborted once this time limit is reached.
        /// </remarks>
        /// <value>
        /// The time to wait for workers to cancel.
        /// </value>
        TimeSpan TimeToWaitForWorkersToCancel { get; set; }

        /// <summary>
        /// Once the queue has begun the shutdown process, we will wait this long before issuing a cancel request to the workers.
        /// </summary>
        /// <value>
        /// The time to wait for workers to stop.
        /// </value>
        TimeSpan TimeToWaitForWorkersToStop { get; set; }

        /// <summary>
        /// If true, worker threads will be aborted if they don't respond to <see cref="TimeToWaitForWorkersToCancel"/>
        /// </summary>
        /// <remarks>Aborting a running thread is generally not a good idea. Don't enable this without understanding what happens when your code is killed. It's better to make sure that the code
        /// executed by the queue will respond to cancel requests in a reasonable amount of time</remarks>
        /// <value>
        /// <c>true</c> if [abort worker threads when stopping]; otherwise, <c>false</c>.
        /// </value>
        bool AbortWorkerThreadsWhenStopping { get; set; }

        /// <summary>
        /// If true, a single worker will be used to look for work when the queue is empty or no valid records are found to process.
        /// When work is located, the other workers will be woken up.
        /// </summary>
        /// <remarks>It's possible for non-polling transports to ignore this setting; the idea is to limit the workers to a single poll if polling is used</remarks>
        /// <value>
        /// <c>true</c> if a single worker should be used when idle; otherwise, <c>false</c>.
        /// </value>
        bool SingleWorkerWhenNoWorkFound { get; set; }
    }
}
