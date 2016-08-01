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
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Sends a job to a SQLite db.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendJobToQueue" />
    public class SqliteSendToJobQueue: ISendJobToQueue
    {
        private readonly IProducerMethodQueue _queue;
        private readonly IQueryHandler<DoesJobExistQuery, QueueStatuses> _doesJobExist;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;
        private readonly IQueryHandler<GetJobIdQuery, long> _getJobId;
        private readonly CreateJobMetaData _createJobMetaData;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSendToJobQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="createJobMetaData">The create job meta data.</param>
        public SqliteSendToJobQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery, QueueStatuses> doesJobExist,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IQueryHandler<GetJobIdQuery, long> getJobId, CreateJobMetaData createJobMetaData)
        {
            _queue = queue;
            _doesJobExist = doesJobExist;
            _deleteMessageCommand = deleteMessageCommand;
            _getJobId = getJobId;
            _createJobMetaData = createJobMetaData;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _queue.IsDisposed;

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueProducerConfiguration Configuration => _queue.Configuration;

        /// <summary>
        /// Sends the specified action to the specified queue.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun)
        {
            var status = CheckStatus(job.Name, scheduledTime);
            return status ??
                   ProcessResult(job, scheduledTime, await _queue.SendAsync(actionToRun, _createJobMetaData.Create(job, scheduledTime)).ConfigureAwait(false));
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
            var status = CheckStatus(job.Name, scheduledTime);
            return status ??
                   ProcessResult(job, scheduledTime, await _queue.SendAsync(expressionToRun, _createJobMetaData.Create(job, scheduledTime)).ConfigureAwait(false));
        }

        private IJobQueueOutputMessage CheckStatus(string name, DateTimeOffset scheduledTime)
        {
            var status = _doesJobExist.Handle(new DoesJobExistQuery(name, scheduledTime));
            switch (status)
            {
                case QueueStatuses.Processing:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedProcessing);
                case QueueStatuses.Waiting:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedWaiting);
                case QueueStatuses.Processed:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyProcessed);
                case QueueStatuses.Error:
                    //delete existing record
                    _deleteMessageCommand.Handle(new DeleteMessageCommand(_getJobId.Handle(new GetJobIdQuery(name))));
                    break;
            }
            return null;
        }
        private IJobQueueOutputMessage ProcessResult(IScheduledJob job, DateTimeOffset scheduledTime, IQueueOutputMessage result)
        {
            if (result.HasError)
            {
                var message = result.SendingException.Message.Replace(Environment.NewLine, " ");
                if ((message.Contains("constraint failed UNIQUE constraint failed:") && message.Contains("JobName")) || message.Contains("Failed to insert record - the job has already been queued or processed"))
                {
                    var status = _doesJobExist.Handle(new DoesJobExistQuery(job.Name, scheduledTime));
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
                    _queue.Dispose();
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
