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
using System.Collections.Generic;

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// An error has occurred while processing a message in user code
    /// </summary>
    /// <remarks>This exception is generated when user message handling code throws an unhanded exception</remarks>
    [Serializable]
    public class MessageException : DotNetWorkQueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        public MessageException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="headers">The message headers.</param>
        public MessageException(string message, IMessageId messageId, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers)
            : base(message)
        {
            MessageId = messageId;
            CorrelationId = correlationId;
            Headers = headers;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="messageId">The message id</param>
        /// <param name="correlationId">the correlation id</param>
        /// <param name="headers">the message headers</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public MessageException(IMessageId messageId, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, string format, params object[] args)
            : base(string.Format(format, args))
        {
            MessageId = messageId;
            CorrelationId = correlationId;
            Headers = headers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="messageId">The message id</param>
        /// <param name="correlationId">the correlation id</param>
        /// <param name="headers">the message headers</param>
        public MessageException(string message, Exception inner, IMessageId messageId, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers)
            : base(message, inner)
        {
            MessageId = messageId;
            CorrelationId = correlationId;
            Headers = headers;
        }

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; }
        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public ICorrelationId CorrelationId { get; }

        /// <summary>
        /// The message headers
        /// </summary>
        public IReadOnlyDictionary<string, object> Headers { get; }
    }
}
