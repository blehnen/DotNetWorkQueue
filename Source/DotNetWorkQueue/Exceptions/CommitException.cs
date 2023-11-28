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
    /// A error has occurred while trying to commit a message
    /// </summary>
    /// <remarks>
    /// This means that the message was processed, but now the transport does not know this.
    /// The same exact message may be sent through for processing again at some point, depending on queue settings.
    /// </remarks>
    [Serializable]
    public class CommitException : MessageException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        public CommitException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException" /> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="messageId">The message id</param>
        /// <param name="correlationId">the correlation id</param>
        /// <param name="headers">the message headers</param>
        public CommitException(string message, IMessageId messageId, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers) :
            base(message, messageId, correlationId, headers)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="messageId">The message id</param>
        /// <param name="correlationId">the correlation id</param>
        /// <param name="headers">the message headers</param>
        public CommitException(string format, IMessageId messageId, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers,
            params object[] args) : base(string.Format(format, args), messageId, correlationId, headers)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        /// <param name="messageId">The message id</param>
        /// <param name="correlationId">the correlation id</param>
        /// <param name="headers">the message headers</param>
        public CommitException(string message, Exception inner, IMessageId messageId, ICorrelationId correlationId,
            IReadOnlyDictionary<string, object> headers) : base(message, inner, messageId, correlationId, headers)
        {

        }
    }
}
