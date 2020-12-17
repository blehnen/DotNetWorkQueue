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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Polly.Bulkhead;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <inheritdoc />
    public class SchedulerTaskFactory : ITaskFactory
    {
        private readonly Lazy<ATaskScheduler> _scheduler;
        private readonly Lazy<TaskFactory> _factory;
        private readonly ILogger _log;
        private readonly object _tryStartNewLocker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerTaskFactory" /> class.
        /// </summary>
        /// <param name="schedulerFactory">The scheduler factory.</param>
        /// <param name="log">The log.</param>
        public SchedulerTaskFactory(ITaskSchedulerFactory schedulerFactory,
            ILogger log)
        {
            Guard.NotNull(() => schedulerFactory, schedulerFactory);
            _scheduler = new Lazy<ATaskScheduler>(schedulerFactory.Create);
            _log = log;
            _factory = new Lazy<TaskFactory>(() =>
            {
                lock (_tryStartNewLocker)
                {
                    return new TaskFactory(_scheduler.Value);
                }
            });
        }

        /// <inheritdoc />
        public TryStartNewResult TryStartNew(Action<object> action, StateInformation state, Action<Task> continueWith, out Task task)
        {
            Guard.NotNull(() => action, action);

            if (SchedulerHasRoom(state))
            {
                lock (_tryStartNewLocker)
                {
                    if (SchedulerHasRoom(state))
                    {
                        try
                        {
                            task = _factory.Value.StartNew(action, state).ContinueWith(continueWith);
                            return TryStartNewResult.Added;
                        }
                        catch (BulkheadRejectedException e)
                        {
                            _log.LogWarning("Failed to enqueue task", e);
                            task = null;
                            return TryStartNewResult.Rejected;
                        }
                    }
                }
            }
            task = null;
            return TryStartNewResult.Rejected;
        }
        /// <inheritdoc />
        public ATaskScheduler Scheduler => _scheduler.Value;

        /// <summary>
        /// Returns true if the scheduler has room for another task
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        private bool SchedulerHasRoom(StateInformation state)
        {
            var result = state.Group == null ? Scheduler.RoomForNewTask : Scheduler.RoomForNewWorkGroupTask(state.Group);
            return result == RoomForNewTaskResult.RoomForTask || result == RoomForNewTaskResult.RoomInQueue;
        }
    }
}
