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

namespace DotNetWorkQueue.JobScheduler
{
    /// <summary>
    /// Creates meta data needed by transports for scheduled jobs
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IJobSchedulerMetaData" />
    public class JobSchedulerMetaData: IJobSchedulerMetaData
    {
        /// <summary>
        /// Sets the specified meta data on the messageData context
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="eventTime">The event time.</param>
        /// <param name="route">The route.</param>
        /// <param name="messageData">The message data.</param>
        public void Set(string jobName, DateTimeOffset scheduledTime, DateTimeOffset eventTime, string route, IAdditionalMessageData messageData)
        {
            messageData.SetSetting("JobName", jobName);
            messageData.SetSetting("JobEventTime", eventTime);
            messageData.SetSetting("JobScheduledTime", scheduledTime);
            messageData.Route = route;
        }

        /// <summary>
        /// Gets the scheduled time.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        public DateTimeOffset GetScheduledTime(IAdditionalMessageData messageData)
        {
            if (messageData.TryGetSetting("JobScheduledTime", out var value))
            {
                return (DateTimeOffset) value;
            }
            return DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Gets the event time.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        public DateTimeOffset GetEventTime(IAdditionalMessageData messageData)
        {
            if (messageData.TryGetSetting("JobEventTime", out var value))
            {
                return (DateTimeOffset)value;
            }
            return DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <param name="messageData">The message data.</param>
        /// <returns></returns>
        public string GetJobName(IAdditionalMessageData messageData)
        {
            if (messageData.TryGetSetting("JobName", out var value))
            {
                return (string)value;
            }
            return string.Empty;
        }
    }
}
