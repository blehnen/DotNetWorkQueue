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

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Status of a message in the history table.
    /// </summary>
    public enum MessageHistoryStatus
    {
        /// <summary>
        /// Message has been enqueued but not yet picked up by a consumer.
        /// </summary>
        Enqueued = 0,

        /// <summary>
        /// Message is currently being processed by a consumer.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Message was processed successfully and committed.
        /// </summary>
        Complete = 2,

        /// <summary>
        /// Message processing failed and the message was moved to the error queue.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Message was explicitly deleted.
        /// </summary>
        Deleted = 4,

        /// <summary>
        /// Message expired before it could be processed.
        /// </summary>
        Expired = 5
    }
}
