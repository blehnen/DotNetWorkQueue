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
using System.Threading.Tasks;

namespace DotNetWorkQueue
{
    /// <summary>
    /// A task scheduler for <see cref="IConsumerQueueScheduler" />
    /// </summary>
    public abstract class ATaskScheduler : TaskScheduler, ITaskScheduler
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public abstract void Start();
        /// <summary>
        /// If true, the task scheduler has room for another task.
        /// </summary>
        /// <remarks>This could mean that a thread is free, or that an in memory queue has room.</remarks>
        /// <value>
        ///   <c>true</c> if [room for new task]; otherwise, <c>false</c>.
        /// </value>
        public abstract RoomForNewTaskResult RoomForNewTask { get; }
        /// <summary>
        /// If true, the task scheduler has room for the specified work group task
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public abstract RoomForNewTaskResult RoomForNewWorkGroupTask(IWorkGroup group);
        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <returns></returns>
        public abstract IWorkGroup AddWorkGroup(string name, int concurrencyLevel);
        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <param name="maxQueueSize">Maximum size of the queue.</param>
        /// <returns></returns>
        public abstract IWorkGroup AddWorkGroup(string name, int concurrencyLevel, int maxQueueSize);
       
        /// <summary>
        /// Adds a new task to the scheduler.
        /// </summary>
        /// <param name="task">The task.</param>
        public abstract void AddTask(Task task);

        /// <summary>
        /// Informs the scheduler that a client is connected
        /// </summary>
        /// <returns></returns>
        public abstract int Subscribe();

        /// <summary>
        /// Informs the scheduler that a client has disconnected
        /// </summary>
        /// <param name="id">The client identifier.</param>
        public abstract void UnSubscribe(int id);

        /// <summary>
        /// Gets a value indicating whether this <see cref="ATaskScheduler"/> is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if started; otherwise, <c>false</c>.
        /// </value>
        public abstract bool Started { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsDisposed { get; }
        
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public abstract ITaskSchedulerConfiguration Configuration { get; }

        /// <summary>
        /// Allows caller to block until a thread is free
        /// </summary>
        /// <remarks>If there are multiple callers, the wait handle may be freed, but not all pending requests will get into the queue.</remarks>
        public abstract IWaitForEventOrCancelThreadPool WaitForFreeThread { get; }
    }
}
