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
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// An internal class holding the result of a dequeue.
    /// </summary>
    internal class RedisMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMessage" /> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="expired">if set to <c>true</c> [expired].</param>
        public RedisMessage(string messageId, IReceivedMessageInternal message, bool expired)
        {
            MessageId = messageId;
            Message = message;
            Expired = expired;
        }
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        /// <remarks>This is the dequeued message</remarks>
        public IReceivedMessageInternal Message { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="RedisMessage"/> is expired.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expired; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If the message has expired, it won't be processed; it will be deleted.</remarks>
        public bool Expired { get; private set; }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        /// <remarks>Can be used to obtain the messageId of expired messages</remarks>
        public string MessageId { get; private set; }
    }
}
