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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Creates metadata needed to add this job to the db.
    /// </summary>
    public class CreateJobMetaData
    {
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobMetaData"/> class.
        /// </summary>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        public CreateJobMetaData(IJobSchedulerMetaData jobSchedulerMetaData)
        {
            _jobSchedulerMetaData = jobSchedulerMetaData;
        }

        /// <summary>
        /// Creates metadata needed to add this job to the db.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="messageData">The message data.</param>
        public void Create(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            IAdditionalMessageData messageData)
        {
            var item = new AdditionalMetaData<string>("JobName", jobName);
            messageData.AdditionalMetaData.Add(item);

            _jobSchedulerMetaData.Set(jobName, scheduledTime, eventTime, messageData);
        }
    }
}
