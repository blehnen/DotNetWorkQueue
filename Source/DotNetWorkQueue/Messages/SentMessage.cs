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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Provides information about a message that was sent
    /// </summary>
    public class SentMessage : ISentMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentMessage"/> class.
        /// </summary>
        /// <param name="messageId">The identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        public SentMessage(IMessageId messageId, ICorrelationId correlationId)
        {
            //NOTE - null messageID's are allowed, as this indicates a failure by the transport to send
            Guard.NotNull(() => correlationId, correlationId);

            MessageId = messageId;
            CorrelationId = correlationId;
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
    }
}
