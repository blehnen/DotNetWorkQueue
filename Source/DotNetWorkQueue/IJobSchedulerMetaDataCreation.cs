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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Creates meta data needed by transports for scheduled jobs
    /// </summary>
    public interface IJobSchedulerMetaData
    {
        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <param name="messageData">The message data.</param>
        void Set(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime,
            string route, IAdditionalMessageData messageData);

        /// <summary>
        /// Gets the scheduled time.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        DateTimeOffset GetScheduledTime(IAdditionalMessageData messageData);
        /// <summary>
        /// Gets the event time.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        DateTimeOffset GetEventTime(IAdditionalMessageData messageData);
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        string GetJobName(IAdditionalMessageData messageData);
    }
}
