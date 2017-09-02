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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Query;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Sends a job to a queue
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ASendJobToQueue" />
    public class RedisSendJobToQueue : ASendJobToQueue
    {
        private readonly IQueryHandler<DoesJobExistQuery, QueueStatuses> _doesJobExist;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, bool> _deleteMessageCommand;
        private readonly IQueryHandler<GetJobIdQuery, string> _getJobId;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisSendJobToQueue" /> class.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        public RedisSendJobToQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery, QueueStatuses> doesJobExist,
            ICommandHandlerWithOutput<DeleteMessageCommand, bool> deleteMessageCommand,
            IQueryHandler<GetJobIdQuery, string> getJobId, 
            IGetTimeFactory getTimeFactory, 
            IJobSchedulerMetaData jobSchedulerMetaData): base(queue, getTimeFactory)
        {
            _doesJobExist = doesJobExist;
            _deleteMessageCommand = deleteMessageCommand;
            _getJobId = getJobId;
            _jobSchedulerMetaData = jobSchedulerMetaData;
        }

        /// <summary>
        /// Returns the status of the job based on name and scheduled time.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
        {
            return _doesJobExist.Handle(new DoesJobExistQuery(name, scheduledTime));
        }

        /// <summary>
        /// Deletes the job based on the job name.
        /// </summary>
        /// <param name="name">The name.</param>
        protected override void DeleteJob(string name)
        {
            _deleteMessageCommand.Handle(new DeleteMessageCommand(new RedisQueueId(_getJobId.Handle(new GetJobIdQuery(name)))));
        }

        /// <summary>
        /// Return true if the exception indicates that the job already exists.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns></returns>
        /// <remarks>
        /// Used to determine if we should return specific error messages
        /// </remarks>
        protected override bool JobAlreadyExistsError(Exception error)
        {
            var message = error.Message.Replace(Environment.NewLine, "");
            return message.Contains("Failed to enqueue a record. The job already exists");
        }

        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route. May be null.</param>
        /// <param name="messageData">The message data.</param>
        protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            string route, IAdditionalMessageData messageData)
        {
            _jobSchedulerMetaData.Set(jobName, scheduledTime, eventTime, route, messageData);
        }
    }
}
