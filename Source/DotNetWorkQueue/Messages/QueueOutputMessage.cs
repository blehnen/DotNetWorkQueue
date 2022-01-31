// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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

namespace DotNetWorkQueue.Messages
{
    /// <inheritdoc />
    public class QueueOutputMessage : IQueueOutputMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueOutputMessage" /> class.
        /// </summary>
        /// <param name="sentMessage">The sent message.</param>
        /// <param name="error">The error.</param>
        public QueueOutputMessage(ISentMessage sentMessage,
            Exception error)
        {
            SentMessage = sentMessage;
            SendingException = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueOutputMessage" /> class.
        /// </summary>
        /// <param name="sentMessage">The sent message.</param>
        public QueueOutputMessage(ISentMessage sentMessage)
        {
            SentMessage = sentMessage;
        }

        /// <inheritdoc />
        public ISentMessage SentMessage { get; }

        /// <inheritdoc />
        public Exception SendingException { get; }

        /// <inheritdoc />
        public bool HasError => SendingException != null || SentMessage == null || !SentMessage.MessageId.HasValue;
    }
}
