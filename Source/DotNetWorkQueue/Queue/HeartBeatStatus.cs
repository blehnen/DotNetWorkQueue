// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Represents the status of a request to update the heart beat for a message
    /// </summary>
    public class HeartBeatStatus: IHeartBeatStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatStatus"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="lastHeartBeatTime">The last heart beat time.</param>
        public HeartBeatStatus(IMessageId messageId, DateTime? lastHeartBeatTime)
        {
            Guard.NotNull(() => messageId, messageId);
            MessageId = messageId;
            LastHeartBeatTime = lastHeartBeatTime;
        }
        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; }
        /// <summary>
        /// Gets the last heart beat time.
        /// </summary>
        /// <value>
        /// The last heart beat time.
        /// </value>
        /// <remarks>
        /// If this value is null, we failed to update the heartbeat. This probably means that the record no longer exists.
        /// </remarks>
        public DateTime? LastHeartBeatTime  { get; }
    }
}
