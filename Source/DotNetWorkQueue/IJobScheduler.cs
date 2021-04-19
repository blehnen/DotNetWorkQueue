// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq.Expressions;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="IIsDisposed" />
    /// <summary>
    /// A reoccurring job scheduler.
    /// </summary>
    public interface IJobScheduler : IDisposable, IIsDisposed
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks>No jobs will be scheduled until start is called</remarks>
        void Start();

#if NETFULL
        /// <summary>
        /// Adds a new job or updates an existing one. Existing jobs must be stopped before being updated.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="jobname">The jobName.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="job">The job.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration. Allows producer specific options to be set.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        IScheduledJob AddUpdateJob<TTransportInit, TQueue>(string jobname,
            QueueConnection queueConnection,
            string schedule,
            LinqExpressionToRun job,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default)
                where TTransportInit : ITransportInit, new()
                where TQueue : class, IJobQueueCreation;

        /// <summary>
        /// Adds a new job or updates an existing one. Existing jobs must be stopped before being updated.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
        /// <param name="queueCreator">The queue creator. Will be used to create the queue if it does not exist.</param>
        /// <param name="jobname">The jobName.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="job">The job.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration. Allows producer specific options to be set.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        IScheduledJob AddUpdateJob<TTransportInit>(IJobQueueCreation queueCreator,
            string jobname,
           QueueConnection queueConnection,
            string schedule,
            LinqExpressionToRun job,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default)
            where TTransportInit : ITransportInit, new();
#endif

        /// <summary>
        /// Adds a new job or updates an existing one. Existing jobs must be stopped before being updated.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="jobName">The jobName.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="job">The job.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration. Allows producer specific options to be set.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <param name="rawExpression">if set to <c>true</c> this expression will not be serialized. This will fail unless an in-process queue is being used.</param>
        /// <returns></returns>
        IScheduledJob AddUpdateJob<TTransportInit, TQueue>(string jobName,
            QueueConnection queueConnection,
            string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> job,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default,
            bool rawExpression = false)
                where TTransportInit : ITransportInit, new()
                where TQueue : class, IJobQueueCreation;

        /// <summary>
        /// Adds a new job or updates an existing one. Existing jobs must be stopped before being updated.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport.</typeparam>
        /// <param name="queueCreator">The queue creator. Will be used to create the queue if it does not exist.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="job">The job.</param>
        /// <param name="route">The route.</param>
        /// <param name="producerConfiguration">The producer configuration. Allows producer specific options to be set.</param>
        /// <param name="autoRun">if set to <c>true</c> [automatic run].</param>
        /// <param name="window">The window.</param>
        /// <param name="rawExpression">if set to <c>true</c> this expression will not be serialized. This will fail unless an in-process queue is being used.</param>
        /// <returns></returns>
        IScheduledJob AddUpdateJob<TTransportInit>(IJobQueueCreation queueCreator,
            string jobName,
            QueueConnection queueConnection,
            string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> job,
            string route = null,
            Action<QueueProducerConfiguration> producerConfiguration = null,
            bool autoRun = true,
            TimeSpan window = default,
            bool rawExpression = false)
            where TTransportInit : ITransportInit, new();

        /// <summary>
        /// Gets all jobs.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IScheduledJob> GetAllJobs();

        /// <summary>
        /// Occurs when a job has thrown an exception when being added to the execution queue.
        /// </summary>
        event Action<IScheduledJob, Exception> OnJobQueueException;
        /// <summary>
        /// Occurs when a job can't be added to the queue; this generally means that a previous job is still running or has already finished for the same scheduled time.
        /// </summary>
        event Action<IScheduledJob, IJobQueueOutputMessage> OnJobNonFatalFailureQueue;
        /// <summary>
        /// Occurs when a job has been added to the execution queue.
        /// </summary>
        event Action<IScheduledJob, IJobQueueOutputMessage> OnJobQueue;

        /// <summary>
        /// Gets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        bool IsShuttingDown { get; }

        /// <summary>
        /// Removes the job from the scheduler
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        bool RemoveJob(string name);
    }
}
