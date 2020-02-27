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
using System.Linq.Expressions;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="IIsDisposed" />
    /// <summary>
    /// Handles scheduling and processing heartbeat requests
    /// </summary>
    public interface IHeartBeatScheduler : IDisposable, IIsDisposed
    {
        /// <summary>
        /// Adds a new job or updates an existing one. Existing jobs must be stopped before being updated.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="job">The job.</param>
        /// <returns></returns>
        IScheduledJob AddUpdateJob(string jobName,
            string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> job);

        /// <summary>
        /// Removes the job from the scheduler
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        bool RemoveJob(string name);

        /// <summary>
        /// Gets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        bool IsShuttingDown { get; }
    }
}
