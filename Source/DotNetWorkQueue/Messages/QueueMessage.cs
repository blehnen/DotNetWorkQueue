// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// A wrapper for a message and it's associated extra data
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TMessageData">The type of the message data.</typeparam>
    public class QueueMessage<TMessage, TMessageData>
        where TMessage: class
        where TMessageData: IAdditionalMessageData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessage{TMessage, TMessageData}"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageData">The message data.</param>
        public QueueMessage(TMessage message, TMessageData messageData)
        {
            Guard.NotNull(() => message, message);
            Message = message;
            MessageData = messageData;
        }
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public TMessage Message { get; }
        /// <summary>
        /// Gets the message data.
        /// </summary>
        /// <value>
        /// The message data.
        /// </value>
        public TMessageData MessageData { get; }
    }
}
