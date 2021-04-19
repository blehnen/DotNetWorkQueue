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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsDisposed => Queue.IsDisposed;

        /// <inheritdoc />
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
        /// <param name="route">The route. May be null.</param>
        /// <param name="messageData">The message data.</param>
        protected abstract void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            string route, IAdditionalMessageData messageData);

        /// <inheritdoc />
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun, bool rawExpression = false)
        {
            var messageData = new AdditionalMessageData();
            var data = StartSend(job, scheduledTime, messageData);
            if (data != null)
                return data;

            var message = await Queue.SendAsync(actionToRun, messageData, rawExpression).ConfigureAwait(false);
            var result = ProcessResult(job, scheduledTime, message);
            if (result != null) return result;
            //try one more time
            result = ProcessResult(job, scheduledTime, await Queue.SendAsync(actionToRun, messageData, rawExpression).ConfigureAwait(false));
            return result ?? new JobQueueOutputMessage(JobQueuedStatus.Failed);
        }

#if NETFULL
        /// <inheritdoc />
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun expressionToRun)
        {
            var messageData = new AdditionalMessageData();
            var data = StartSend(job, scheduledTime, messageData);
            if (data != null)
                return data;

            var message = await Queue.SendAsync(expressionToRun, messageData).ConfigureAwait(false);
            var result = ProcessResult(job, scheduledTime, message);
            if (result != null) return result;
            //try one more time
            result = ProcessResult(job, scheduledTime,
                await Queue.SendAsync(expressionToRun, messageData).ConfigureAwait(false));
            return result ?? new JobQueueOutputMessage(JobQueuedStatus.Failed);
        }
#endif
        /// <summary>
        /// Begins the send process, if possible
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="messageData">The message data.</param>
        /// <returns>Null if we should continue; a <see cref="IJobQueueOutputMessage"/> object if the send should be aborted </returns>
        private IJobQueueOutputMessage StartSend(IScheduledJob job, DateTimeOffset scheduledTime, IAdditionalMessageData messageData)
        {
            var data = SendPreChecks(job.Name, scheduledTime);
            if (data != null)
                return data;

            SetMetaDataForJob(job.Name, scheduledTime,
                new DateTimeOffset(GetTimeFactory.Create().GetCurrentUtcDate()), job.Route, messageData);

            return null;
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
            //no errors, so just return
            if (!result.HasError) return new JobQueueOutputMessage(result, JobQueuedStatus.Success);

            //this is not an error for the job already existing in the queue
            if (!JobAlreadyExistsError(result.SendingException))
                return new JobQueueOutputMessage(result, JobQueuedStatus.Failed);

            var status = DoesJobExist(job.Name, scheduledTime);
            switch (status)
            {
                case QueueStatuses.Processing:
                    return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedProcessing);
                case QueueStatuses.Waiting:
                    return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedWaiting);
                case QueueStatuses.Processed:
                    return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyProcessed);
                case QueueStatuses.Error:
                    DeleteJob(job.Name);
                    return null;
                default:
                    return null; //try to re-queue once; if this is second try and this happens again, an error will be returned
            }
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
