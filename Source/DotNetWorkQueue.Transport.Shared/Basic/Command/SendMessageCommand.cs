// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic.Command
{
    /// <summary>
    /// Sends a new message to a queue
    /// </summary>
    public class SendMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommand"/> class.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="messageData">The message data.</param>
        public SendMessageCommand(IMessage messageToSend, IAdditionalMessageData messageData)
        {
            Guard.NotNull(() => messageToSend, messageToSend);
            Guard.NotNull(() => messageData, messageData);

            MessageData = messageData;
            MessageToSend = messageToSend;
        }
        /// <summary>
        /// Gets or sets the message to send.
        /// </summary>
        /// <value>
        /// The message to send.
        /// </value>
        public IMessage MessageToSend { get; }
        /// <summary>
        /// Gets or sets the message data.
        /// </summary>
        /// <value>
        /// The message data.
        /// </value>
        public IAdditionalMessageData MessageData { get; }
        /// <summary>
        /// Optional caller-supplied transaction for the outbox pattern. When set, the relational
        /// transport's send-message handler skips its internal connection/transaction management
        /// and uses this transaction's connection and transaction reference instead. When null
        /// (the default), the transport manages its own connection and transaction lifecycle
        /// exactly as before.
        /// </summary>
        /// <remarks>
        /// Wired to the bypass mechanism via <c>RelationalSendMessageCommand</c> (a derived class
        /// in <c>Transport.RelationalDatabase</c>) which exposes <c>SkipRetry</c> through
        /// <c>IRetrySkippable</c>. The base <c>SendMessageCommand</c> itself does NOT implement
        /// <c>IRetrySkippable</c> to keep <c>Transport.Shared</c> free of references to
        /// <c>Transport.RelationalDatabase</c>.
        /// </remarks>
        public DbTransaction ExternalTransaction { get; init; }
    }
}
