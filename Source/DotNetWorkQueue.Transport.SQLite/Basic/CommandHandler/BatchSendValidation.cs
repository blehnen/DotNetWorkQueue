// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Shared validation for the batch send handlers (sync and async).
    /// </summary>
    internal static class BatchSendValidation
    {
        /// <summary>
        /// Throws if any message in the batch is a scheduled job. The batch path has no
        /// equivalent of the per-message job-uniqueness query, so scheduled jobs must be sent
        /// individually via <c>Send(message)</c>.
        /// </summary>
        public static void GuardNoScheduledJobs(
            IReadOnlyList<QueueMessage<IMessage, IAdditionalMessageData>> messages,
            IJobSchedulerMetaData jobSchedulerMetaData)
        {
            foreach (var m in messages)
            {
                if (!string.IsNullOrWhiteSpace(jobSchedulerMetaData.GetJobName(m.MessageData)))
                {
                    throw new NotSupportedException(
                        "Batch send does not support scheduled jobs; send scheduled jobs individually " +
                        "via Send(message).");
                }
            }
        }
    }
}
