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
using System.Threading.Tasks;
namespace DotNetWorkQueue
{
    /// <summary>
    /// A task scheduler for <see cref="IConsumerQueueScheduler" />
    /// </summary>
    public interface ITaskScheduler: IDisposable, IIsDisposed
    {
        /// <summary>
        /// If true, the task scheduler has room for another non work group task.
        /// </summary>
        /// <remarks>This could mean that a thread is free, or that an in memory queue has room.</remarks>
        /// <value>
        /// <see cref="RoomForNewTaskResult"/>
        /// </value>
        RoomForNewTaskResult RoomForNewTask { get; }
        /// <summary>
        /// If true, the task scheduler has room for the specified work group task
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns><see cref="RoomForNewTaskResult"/></returns>
        RoomForNewTaskResult RoomForNewWorkGroupTask(IWorkGroup group);
        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <returns></returns>
        IWorkGroup AddWorkGroup(string name, int concurrencyLevel);
        /// <summary>
        /// Adds a new work group.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        /// <param name="maxQueueSize">Maximum size of the queue.</param>
        /// <returns></returns>
        IWorkGroup AddWorkGroup(string name, int concurrencyLevel, int maxQueueSize);
        /// <summary>
        /// Adds a new task to the scheduler.
        /// </summary>
        /// <param name="task">The task.</param>
        void AddTask(Task task);
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        ITaskSchedulerConfiguration Configuration { get; }
        /// <summary>
        /// Allows caller to block until a thread is free
        /// </summary>
        /// <remarks>If there are multiple callers, the wait handle may be freed, but not all pending requests will get into the queue.</remarks>
        IWaitForEventOrCancelThreadPool WaitForFreeThread { get; }
    }

    /// <summary>
    /// Indicates the result of a request to check for scheduler capacity
    /// </summary>
    public enum RoomForNewTaskResult
    {
        /// <summary>
        /// The result was not specified
        /// </summary>
        NotSpecified = 0,
        /// <summary>
        /// There is room for a new task to start
        /// </summary>
        RoomForTask = 1,
        /// <summary>
        /// Task queue is full, but the memory queue has room
        /// </summary>
        RoomInQueue = 2,
        /// <summary>
        /// There is no room
        /// </summary>
        No = 3
    }
}
