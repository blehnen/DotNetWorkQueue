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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Sends jobs to a transport
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IProducerMethodJobQueue" />
    public class ProducerMethodJobQueue : IProducerMethodJobQueue
    {
        private readonly ISendJobToQueue _sendJobToQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerMethodJobQueue" /> class.
        /// </summary>
        /// <param name="jobSchedulerLastKnownEvent">The job scheduler last known event.</param>
        /// <param name="sendJobToQueue">The send job to queue.</param>
        /// <param name="logFactory">The log factory.</param>
        /// <param name="createJobQueue">The create job queue.</param>
        public ProducerMethodJobQueue(IJobSchedulerLastKnownEvent jobSchedulerLastKnownEvent,
            ISendJobToQueue sendJobToQueue,
            ILogFactory logFactory,
            IJobTableCreation createJobQueue)
        {
            Guard.NotNull(() => jobSchedulerLastKnownEvent, jobSchedulerLastKnownEvent);
            Guard.NotNull(() => sendJobToQueue, sendJobToQueue);
            Guard.NotNull(() => logFactory, logFactory);
            Guard.NotNull(() => createJobQueue, createJobQueue);

            LastKnownEvent = jobSchedulerLastKnownEvent;
            _sendJobToQueue = sendJobToQueue;
            Logger = logFactory.Create();

            if (!createJobQueue.JobTableExists)
            {
                createJobQueue.CreateJobTable();
            }
        }
        /// <summary>
        /// Gets the last known event.
        /// </summary>
        public IJobSchedulerLastKnownEvent LastKnownEvent { get; }

        /// <summary>
        /// Gets the queue specific logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILog Logger { get; }

        /// <summary>
        /// Sends the specified dynamic linqExpression to be executed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="linqExpression">The linqExpression to execute.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun linqExpression)
        {
            return await _sendJobToQueue.SendAsync(job, scheduledTime, linqExpression).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the specified linqExpression to be executed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="method">The linqExpression to execute.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method)
        {
            return await _sendJobToQueue.SendAsync(job, scheduledTime, method).ConfigureAwait(false);
        }

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueProducerConfiguration Configuration => _sendJobToQueue.Configuration;

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _sendJobToQueue.IsDisposed;

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
                    _sendJobToQueue.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}
