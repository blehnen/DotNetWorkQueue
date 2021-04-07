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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Gets the last known event time for a job
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobSchedulerLastKnownEvent" />
    internal class LiteDbJobSchedulerLastKnownEvent: IJobSchedulerLastKnownEvent
    {
        private readonly IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> _queryGetJobTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbJobSchedulerLastKnownEvent" /> class.
        /// </summary>
        /// <param name="queryGetJobTime">The query get job time.</param>
        public LiteDbJobSchedulerLastKnownEvent(IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset> queryGetJobTime)
        {
            _queryGetJobTime = queryGetJobTime;
        }

        /// <summary>
        /// Gets the last known event time for the specified job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        public DateTimeOffset Get(string jobName)
        {
            return _queryGetJobTime.Handle(new GetJobLastKnownEventQuery(jobName));
        }
    }
}
