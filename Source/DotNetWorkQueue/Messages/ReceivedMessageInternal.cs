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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// The internal representation of a received message.
    /// </summary>
    /// <remarks>This class is used so that we don't need to know the type of the body everywhere. Otherwise, the
    /// generic type would need to flow all the way down to the end of the dependency chain.
    /// </remarks>
    public class ReceivedMessageInternal : IReceivedMessageInternal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IReceivedMessage{T}" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        public ReceivedMessageInternal(IMessage message, IMessageId messageId, ICorrelationId correlationId)
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => messageId, messageId);
            Guard.NotNull(() => correlationId, correlationId);

            Body = message.Body;
            MessageId = messageId;
            CorrelationId = correlationId;
            Headers = new ReadOnlyDictionary<string, object>(message.Headers.ToDictionary(entry => entry.Key,
                                               entry => entry.Value));
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
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
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public dynamic Body { get; }
    }
}
