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
namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Returns a message back to a waiting for processing state
    /// </summary>
    public class RollbackMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="increaseQueueDelay">The increase queue delay.</param>
        public RollbackMessageCommand(RedisQueueId id, TimeSpan? increaseQueueDelay)
        {
            Guard.NotNull(() => id, id);
            Id = id;
            IncreaseQueueDelay = increaseQueueDelay;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get; private set; }
        /// <summary>
        /// Gets the increase queue delay.
        /// </summary>
        /// <value>
        /// The increase queue delay.
        /// </value>
        public TimeSpan? IncreaseQueueDelay { get; private set; }
    }
}
