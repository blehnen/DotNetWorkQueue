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
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Represents a job that has been scheduled.
    /// </summary>
    public interface IScheduledJob
    {
        /// <summary>
        /// Occurs when a job has thrown an exception when being added to the execution queue.
        /// </summary>
        event Action<IScheduledJob, Exception> OnException;
        /// <summary>
        /// Occurs when a job can't be added to the queue; this generally means that a previous job is still running or has already finished for the same scheduled time.
        /// </summary>
        event Action<IScheduledJob, IJobQueueOutputMessage> OnNonFatalFailureEnQueue;
        /// <summary>
        /// Occurs when a job has been added to the execution queue.
        /// </summary>
        event Action<IScheduledJob, IJobQueueOutputMessage> OnEnQueue;

        /// <summary>
        /// Gets the job name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the schedule.
        /// </summary>
        /// <value>
        /// The schedule.
        /// </value>
        IJobSchedule Schedule { get; }

        /// <summary>
        /// Gets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        string Route { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is schedule running; otherwise, <c>false</c>.
        /// </value>
        bool IsScheduleRunning { get; }

        /// <summary>
        /// Gets a value indicating whether the schedule is being added to the queue.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is callback executing; otherwise, <c>false</c>.
        /// </value>
        bool IsCallbackExecuting { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is attached.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is attached; otherwise, <c>false</c>.
        /// </value>
        bool IsAttached { get; set; }
        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>
        /// The window.
        /// </value>
        TimeSpan Window { get; }
        /// <summary>
        /// Gets the next event.
        /// </summary>
        /// <value>
        /// The next event.
        /// </value>
        DateTimeOffset NextEvent { get; }
        /// <summary>
        /// Gets the previous event.
        /// </summary>
        /// <value>
        /// The previous event.
        /// </value>
        DateTimeOffset PrevEvent { get; }
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        ILog Logger { get; }

        /// <summary>
        /// Starts the schedule.
        /// </summary>
        void StartSchedule();
        /// <summary>
        /// Stops the schedule.
        /// </summary>
        void StopSchedule();
        /// <summary>
        /// Updates the schedule.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        void UpdateSchedule(string schedule);
        /// <summary>
        /// Updates the schedule.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        void UpdateSchedule(IJobSchedule schedule);
    }
}
