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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Sends a job to a SQLite db.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ASendJobToQueue" />
    public class SqliteSendToJobQueue: ASendJobToQueue
    {
        private readonly IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> _doesJobExist;
        private readonly IRemoveMessage _removeMessage;
        private readonly IQueryHandler<GetJobIdQuery, long> _getJobId;
        private readonly CreateJobMetaData _createJobMetaData;

        /// <summary>Initializes a new instance of the <see cref="SqliteSendToJobQueue"/> class.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="removeMessage">The remove message.</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="createJobMetaData">The create job meta data.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public SqliteSendToJobQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> doesJobExist,
            IRemoveMessage removeMessage,
            IQueryHandler<GetJobIdQuery, long> getJobId, CreateJobMetaData createJobMetaData,
            IGetTimeFactory getTimeFactory): base(queue, getTimeFactory)
        {
            _doesJobExist = doesJobExist;
            _removeMessage = removeMessage;
            _getJobId = getJobId;
            _createJobMetaData = createJobMetaData;
        }

        /// <summary>
        /// Returns the status of the job based on name and scheduled time.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
        {
           return _doesJobExist.Handle(new DoesJobExistQuery<IDbConnection, IDbTransaction>(name, scheduledTime));
        }

        /// <summary>
        /// Deletes the job based on the job name.
        /// </summary>
        /// <param name="name">The name.</param>
        protected override void DeleteJob(string name)
        {
            _removeMessage.Remove(new MessageQueueId(_getJobId.Handle(new GetJobIdQuery(name))), RemoveMessageReason.Error);
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
            var message = error.Message.Replace(Environment.NewLine, " ");
            return message.Contains("constraint failed UNIQUE constraint failed:") && message.Contains("JobName") ||
                    message.Contains("Failed to insert record - the job has already been queued or processed");
        }

        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <param name="messageData">The message data.</param>
        protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route,
            IAdditionalMessageData messageData)
        {
            _createJobMetaData.Create(jobName, scheduledTime, eventTime,  messageData, route);
        }
    }
}
