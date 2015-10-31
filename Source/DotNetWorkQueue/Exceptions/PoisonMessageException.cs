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

using System;
namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// A poison message has beeen pulled from a transport. A poison message can be read from the transport, but can't be re-created.
    /// </summary>
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
        public PoisonMessageException(string message, IMessageId messageId, ICorrelationId correlationId)
            : base(message, messageId, correlationId)
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public PoisonMessageException(IMessageId messageId, ICorrelationId correlationId, string format, params object[] args)
            : base(string.Format(format, args), messageId, correlationId)
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        public PoisonMessageException(string message, Exception inner, IMessageId messageId, ICorrelationId correlationId)
            : base(message, inner, messageId, correlationId)
        {

        }
    }
}
