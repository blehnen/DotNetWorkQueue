﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Npgsql;
using CreateJobMetaData = DotNetWorkQueue.Transport.Shared.Basic.CreateJobMetaData;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlSendJobToQueue : ASendJobToQueue
    {
        private readonly IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> _doesJobExist;
        private readonly IQueryHandler<GetJobIdQuery<long>, long> _getJobId;
        private readonly CreateJobMetaData _createJobMetaData;
        private readonly IRemoveMessage _removeMessage;

        /// <summary>Initializes a new instance of the <see cref="PostgreSqlSendJobToQueue"/> class.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="doesJobExist">Query for determining if a job already exists</param>
        /// <param name="getJobId">The get job identifier.</param>
        /// <param name="createJobMetaData">The create job meta data.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="removeMessage"></param>
        /// <inheritdoc />
        public PostgreSqlSendJobToQueue(IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> doesJobExist,
            IQueryHandler<GetJobIdQuery<long>, long> getJobId,
            CreateJobMetaData createJobMetaData,
            IGetTimeFactory getTimeFactory,
            IRemoveMessage removeMessage) : base(queue, getTimeFactory)
        {
            _doesJobExist = doesJobExist;
            _getJobId = getJobId;
            _createJobMetaData = createJobMetaData;
            _removeMessage = removeMessage;
        }

        /// <inheritdoc />
        protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
        {
            return _doesJobExist.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(name, scheduledTime));
        }

        /// <inheritdoc />
        protected override void DeleteJob(string name)
        {
            _removeMessage.Remove(new MessageQueueId<long>(_getJobId.Handle(new GetJobIdQuery<long>(name))), RemoveMessageReason.Error);
        }

        /// <inheritdoc />
        protected override bool JobAlreadyExistsError(Exception error)
        {
            var message = error.Message.Replace(Environment.NewLine, " ");
            return message.Contains("duplicate key value violates unique constraint") && message.Contains("jobname") || message.Contains("Failed to insert record - the job has already been queued or processed");
        }

        /// <inheritdoc />
        protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            string route, IAdditionalMessageData messageData)
        {
            _createJobMetaData.Create(jobName, scheduledTime, eventTime, messageData, route);
        }
    }
}
