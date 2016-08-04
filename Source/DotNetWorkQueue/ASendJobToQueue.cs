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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Base class for sending jobs to transports
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendJobToQueue" />
    public abstract class ASendJobToQueue: ISendJobToQueue
    {
        /// <summary>
        /// The queue to send jobs to.
        /// </summary>
        protected readonly IProducerMethodQueue Queue;
        /// <summary>
        /// Time factory for obtaining current date/time
        /// </summary>
        protected readonly IGetTimeFactory GetTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ASendJobToQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        protected ASendJobToQueue(IProducerMethodQueue queue, 
            IGetTimeFactory getTimeFactory)
        {
            Queue = queue;
            GetTimeFactory = getTimeFactory;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Queue.IsDisposed;

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueProducerConfiguration Configuration => Queue.Configuration;

        /// <summary>
        /// Returns the status of the job based on name and scheduled time.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        protected abstract QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime);

        /// <summary>
        /// Deletes the job based on the job name.
        /// </summary>
        /// <param name="name">The name.</param>
        protected abstract void DeleteJob(string name);

        /// <summary>
        /// Return true if the exception indicates that the job already exists.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns></returns>
        /// <remarks>Used to determine if we should return specific error messages</remarks>
        protected abstract bool JobAlreadyExistsError(Exception error);

        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="messageData">The message data.</param>
        protected abstract void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            IAdditionalMessageData messageData);

        /// <summary>
        /// Sends the specified action to the specified queue.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun)
        {
            var data = SendPreChecks(job.Name, scheduledTime);
            if (data != null)
                return data;

            var messageData = new AdditionalMessageData();
            SetMetaDataForJob(job.Name, scheduledTime,
                new DateTimeOffset(GetTimeFactory.Create().GetCurrentUtcDate()), messageData);
            return ProcessResult(job, scheduledTime, await Queue.SendAsync(actionToRun, messageData).ConfigureAwait(false));
        }

        /// <summary>
        /// Sends the specified action to the specified queue.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="expressionToRun">The expression to run.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun expressionToRun)
        {
            var data = SendPreChecks(job.Name, scheduledTime);
            if (data != null)
                return data;

            var messageData = new AdditionalMessageData();
            SetMetaDataForJob(job.Name, scheduledTime,
                new DateTimeOffset(GetTimeFactory.Create().GetCurrentUtcDate()), messageData);
            return ProcessResult(job, scheduledTime, await Queue.SendAsync(expressionToRun, messageData).ConfigureAwait(false));
        }

        private IJobQueueOutputMessage SendPreChecks(string jobName, DateTimeOffset scheduledTime)
        {
            var status = DoesJobExist(jobName, scheduledTime);
            switch (status)
            {
                case QueueStatuses.Processing:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedProcessing);
                case QueueStatuses.Waiting:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedWaiting);
                case QueueStatuses.Processed:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyProcessed);
                case QueueStatuses.Error:
                    DeleteJob(jobName);
                    break;
            }
            return null;
        }
        private IJobQueueOutputMessage ProcessResult(IScheduledJob job, DateTimeOffset scheduledTime, IQueueOutputMessage result)
        {
            if (result.HasError)
            {
                if (JobAlreadyExistsError(result.SendingException))
                {
                    var status = DoesJobExist(job.Name, scheduledTime);
                    switch (status)
                    {
                        case QueueStatuses.Processing:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedProcessing);
                        case QueueStatuses.Waiting:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedWaiting);
                        case QueueStatuses.Processed:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyProcessed);
                        default:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.Failed);
                    }
                }
                return new JobQueueOutputMessage(result, JobQueuedStatus.Failed);
            }
            return new JobQueueOutputMessage(result, JobQueuedStatus.Success);
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Queue.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
