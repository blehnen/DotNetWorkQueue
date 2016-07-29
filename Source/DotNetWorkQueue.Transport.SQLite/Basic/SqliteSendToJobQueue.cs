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
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISendJobToQueue" />
    public class SqliteSendToJobQueue: ISendJobToQueue
    {
        private readonly IProducerMethodQueue _queue;
        private readonly IQueryHandler<DoesJobExistQuery, QueueStatus> _doesJobExist;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;
        private readonly IQueryHandler<GetJobIdQuery, long> _getJobId;
        private readonly IGetTimeFactory _getTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSendToJobQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SqliteSendToJobQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery, QueueStatus> doesJobExist,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IQueryHandler<GetJobIdQuery, long> getJobId, IGetTimeFactory getTimeFactory)
        {
            _queue = queue;
            _doesJobExist = doesJobExist;
            _deleteMessageCommand = deleteMessageCommand;
            _getJobId = getJobId;
            _getTimeFactory = getTimeFactory;
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
            var status = _doesJobExist.Handle(new DoesJobExistQuery(job.Name, scheduledTime));
            switch (status)
            {
                case QueueStatus.Processing:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedProcessing);
                case QueueStatus.Waiting:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedWaiting);
                case QueueStatus.Error:
                    //delete existing record - will re-queue and re-run
                    _deleteMessageCommand.Handle(new DeleteMessageCommand(_getJobId.Handle(new GetJobIdQuery(job.Name))));
                    break;
            }
            return ProcessResult(job, scheduledTime, await _queue.SendAsync(actionToRun, CreateAdditionalData(job, scheduledTime)).ConfigureAwait(false));
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
            var status = _doesJobExist.Handle(new DoesJobExistQuery(job.Name, scheduledTime));
            switch (status)
            {
                case QueueStatus.Processing:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedProcessing);
                case QueueStatus.Waiting:
                    return new JobQueueOutputMessage(JobQueuedStatus.AlreadyQueuedWaiting);
                case QueueStatus.Error:
                    //delete existing record
                    _deleteMessageCommand.Handle(new DeleteMessageCommand(_getJobId.Handle(new GetJobIdQuery(job.Name))));
                    break;
            }
            return ProcessResult(job, scheduledTime, await _queue.SendAsync(expressionToRun, CreateAdditionalData(job, scheduledTime)).ConfigureAwait(false));
        }
        private IJobQueueOutputMessage ProcessResult(IScheduledJob job, DateTimeOffset scheduledTime, IQueueOutputMessage result)
        {
            if (result.HasError)
            {
                var message = result.SendingException.Message.Replace(System.Environment.NewLine, " ");
                if ((message.Contains("constraint failed UNIQUE constraint failed:") && message.Contains("JobName")) || message.Contains("Failed to insert record - the job has already been queued or processed"))
                {
                    var status = _doesJobExist.Handle(new DoesJobExistQuery(job.Name, scheduledTime));
                    switch (status)
                    {
                        case QueueStatus.Processing:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedProcessing);
                        case QueueStatus.Waiting:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.AlreadyQueuedWaiting);
                        default:
                            return new JobQueueOutputMessage(result, JobQueuedStatus.Failed);
                    }
                }
                return new JobQueueOutputMessage(result, JobQueuedStatus.Failed);
            }
            return new JobQueueOutputMessage(result, JobQueuedStatus.Success);
        }

        private IAdditionalMessageData CreateAdditionalData(IScheduledJob job, DateTimeOffset scheduledTime)
        {
            var additionalData = new AdditionalMessageData();
            var item = new AdditionalMetaData<string>("JobName", job.Name);
            additionalData.AdditionalMetaData.Add(item);

            var item2 = new AdditionalMetaData<DateTimeOffset>("@JobEventTime", new DateTimeOffset(_getTimeFactory.Create().GetCurrentUtcDate()));
            additionalData.AdditionalMetaData.Add(item2);

            var item3 = new AdditionalMetaData<DateTimeOffset>("@JobScheduledTime", scheduledTime);
            additionalData.AdditionalMetaData.Add(item3);

            return additionalData;
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
