// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Contains meta data about a message
    /// </summary>
    /// <remarks>This data is stored in a hash, seperate from the message itself</remarks>
    internal class RedisMetaData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMetaData" /> class.
        /// </summary>
        /// <param name="queueDateTime">The queue date time.</param>
        public RedisMetaData(long queueDateTime)
        {
            ErrorTracking = new RedisErrorTracking();
            QueueDateTime = queueDateTime;
        }
        /// <summary>
        /// Gets the queue date time.
        /// </summary>
        /// <value>
        /// The queue date time.
        /// </value>
        /// <remarks>Unix timestamp (MS) of when this record was enqueued</remarks>
        public long QueueDateTime { get; private set; }
        /// <summary>
        /// Gets the error tracking.
        /// </summary>
        /// <value>
        /// The error tracking.
        /// </value>
        public RedisErrorTracking ErrorTracking { get; set; }
    }
}
