// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Handles message processing for the task scheduler queue
    /// </summary>
    public class SchedulerMessageHandler
    {
        private readonly ILog _log;
        private readonly ICounter _waitingOnFreeThreadCounter;
        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerMessageHandler"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="metrics">metrics factory</param>
        public SchedulerMessageHandler(ILogFactory log,
            IMetrics metrics)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => metrics, metrics);

            _log = log.Create();

            var name = GetType().Name;
            _waitingOnFreeThreadCounter = metrics.Counter($"{name}.WaitingOnTaskCounter", Units.Items);
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <typeparam name="T">the type of the message.</typeparam>
        /// <param name="workGroup">The work group.</param>
        /// <param name="message">The message.</param>
        /// <param name="notifications">The notifications.</param>
        /// <param name="functionToRun">The function to run.</param>
        /// <param name="taskFactory">The task factory.</param>
        /// <returns></returns>
        public Task Handle<T>(IWorkGroup workGroup, IReceivedMessage<T> message, IWorkerNotification notifications, Action<IReceivedMessage<T>, IWorkerNotification> functionToRun, ITaskFactory taskFactory) 
            where T : class
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => notifications, notifications);
            Guard.NotNull(() => functionToRun, functionToRun);
            Guard.NotNull(() => taskFactory, taskFactory);

            while (true)
            {
                //verify that we are not canceling or stopping before trying to queue the item
                //however, the transport must support rollbacks
                if (!ShouldHandle(notifications))
                {
                    return null;
                }

                Task start;
                if (taskFactory.TryStartNew(state => { WrappedFunction(message, notifications, functionToRun); }, new StateInformation(workGroup), task =>
                {
                    if (task.IsFaulted && task.Exception?.InnerException is OperationCanceledException)
                    {
                        //bubble the cancel exception; the queue will rollback the message if possible
                        throw new OperationCanceledException("user canceled", task.Exception.InnerException); //explicitly throw this
                    }

                    if (task.IsFaulted && task.Exception != null)
                    {
                        //need to throw it
                        throw new DotNetWorkQueueException("Message processing exception", task.Exception.InnerException);
                    }
                }, out start).Success())
                {
                    try
                    {
                        return start;
                    }
                    finally
                    {
                        //block here if the scheduler is full
                        try
                        {
                            _waitingOnFreeThreadCounter.Increment();
                            taskFactory.Scheduler.WaitForFreeThread.Wait(workGroup);
                        }
                        finally
                        {
                            _waitingOnFreeThreadCounter.Decrement();
                        }
                    }
                }

                //block if the scheduler is full
                try
                {
                    _waitingOnFreeThreadCounter.Increment();
                    taskFactory.Scheduler.WaitForFreeThread.Wait(workGroup);
                }
                finally
                {
                    _waitingOnFreeThreadCounter.Decrement();
                }
            }
        }

        /// <summary>
        /// Checks to see if the queue is stopping; if not, runs the user provided message processing delegate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <param name="notifications">The notifications.</param>
        /// <param name="functionToRun">The function to run.</param>
        private void WrappedFunction<T>(IReceivedMessage<T> message, IWorkerNotification notifications, Action<IReceivedMessage<T>, IWorkerNotification> functionToRun)
            where T : class
        {
            if (ShouldHandle(notifications))
            {
                functionToRun(message, notifications);
            }
        }

        /// <summary>
        /// Checks to see if message processing should happen; will throw an exception if not
        /// </summary>
        /// <param name="notifications">The notifications.</param>
        /// <returns>Always true; exception is thrown for false</returns>
        /// <exception cref="System.OperationCanceledException"></exception>
        private bool ShouldHandle(IWorkerNotification notifications)
        {
            if (notifications.TransportSupportsRollback &&
                notifications.WorkerStopping.Tokens.Any(m => m.IsCancellationRequested))
            {
                _log.Info("System is preparing to stop - aborting");
                notifications.WorkerStopping.Tokens.Find(m => m.IsCancellationRequested).ThrowIfCancellationRequested();
            }

            if (notifications.TransportSupportsRollback && notifications.HeartBeat != null && notifications.HeartBeat.ExceptionHasOccured.IsCancellationRequested)
            {
                _log.Warn(
                    "The heartbeat worker has failed - aborting our work since another thread may pick up this item");
                notifications.HeartBeat.ExceptionHasOccured.ThrowIfCancellationRequested();
            }

            return true;
        }
    }
}
