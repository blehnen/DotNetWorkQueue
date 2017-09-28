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

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Contains worker configuration settings
    /// </summary>
    public class WorkerConfiguration : IWorkerConfiguration
    {
        private int _workerCount;
        private TimeSpan _timeToWaitForWorkersToCancel;
        private TimeSpan _timeToWaitForWorkersToStop;
        private bool _abortWorkerThreadsWhenStopping;
        private bool _singleWorkerWhenNoWorkFound;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerConfiguration"/> class.
        /// </summary>
        public WorkerConfiguration()
        {
            WorkerCount = 1;
            SingleWorkerWhenNoWorkFound = true;
            TimeToWaitForWorkersToCancel = TimeSpan.FromSeconds(10);
            TimeToWaitForWorkersToStop = TimeSpan.FromSeconds(15);
        }
        #endregion

        #region Configuration

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
        public int WorkerCount
        {
            get => _workerCount;
            set
            {
                FailIfReadOnly();
                _workerCount = value;
            }
        }
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
        public TimeSpan TimeToWaitForWorkersToCancel
        {
            get => _timeToWaitForWorkersToCancel;
            set
            {
                FailIfReadOnly();
                _timeToWaitForWorkersToCancel = value;
            }
        }
        /// <summary>
        /// Once the queue has begun the shutdown process, we will wait this long before issuing a cancel request to the workers.
        /// </summary>
        /// <value>
        /// The time to wait for workers to stop.
        /// </value>
        public TimeSpan TimeToWaitForWorkersToStop
        {
            get => _timeToWaitForWorkersToStop;
            set
            {
                FailIfReadOnly();
                _timeToWaitForWorkersToStop = value;
            }
        }

        /// <summary>
        /// If true, worker threads will be aborted if they don't respond to <see cref="TimeToWaitForWorkersToCancel"/>
        /// </summary>
        /// <remarks>Aborting a running thread is generally not a good idea. Don't enable this without understanding what happens when your code is killed. It's better to make sure that the code
        /// executed by the queue will respond to cancel requests in a reasonable amount of time</remarks>
        /// <value>
        /// <c>true</c> if [abort worker threads when stopping]; otherwise, <c>false</c>.
        /// </value>
        public bool AbortWorkerThreadsWhenStopping
        {
            get => _abortWorkerThreadsWhenStopping;
            set
            {
                FailIfReadOnly();
                _abortWorkerThreadsWhenStopping = value;
            }
        }

        /// <summary>
        /// If true, a single worker will be used to look for work when the queue is empty or no valid records are found to process.
        /// When work is located, the other workers will be woken up.
        /// </summary>
        /// <value>
        /// <c>true</c> if a single worker should be used when idle; otherwise, <c>false</c>.
        /// </value>
        public bool SingleWorkerWhenNoWorkFound
        {
            get => _singleWorkerWhenNoWorkFound;
            set
            {
                FailIfReadOnly();
                _singleWorkerWhenNoWorkFound = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public virtual void SetReadOnly()
        {
            IsReadOnly = true;
        }
        #endregion
    }
}
