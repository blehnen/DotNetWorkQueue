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
using System.Runtime.Serialization;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// A poison message has been pulled from a transport. A poison message can be read from the transport, but can't be re-created.
    /// </summary>
    /// <remarks>
    /// When possible, all 'standard' data is included with the exception. Transport specific data is generally not included.
    /// For instance, user defined columns from the SQL server transport are not included.
    /// </remarks>
    [Serializable]
    public class PoisonMessageException: MessageException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        public PoisonMessageException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        public PoisonMessageException(string message, IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload)
            : base(message, messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public PoisonMessageException(IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload, string format, params object[] args)
            : base(string.Format(format, args), messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        public PoisonMessageException(string message, Exception inner, IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload)
            : base(message, inner, messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }

        /// <summary>
        /// The raw bytes of the serialized poison message
        /// </summary>
        /// <value>
        /// The message payload.
        /// </value>
        public byte[] MessagePayload { get; }

        /// <summary>
        /// The raw bytes of the header payload.
        /// </summary>
        /// <value>
        /// The header payload.
        /// </value>
        public byte[] HeaderPayload { get; }
    }
}
