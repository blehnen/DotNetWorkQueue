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
using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic.Command
{
    /// <summary>
    /// Sends a batch of new messages to a queue in a single operation.
    /// </summary>
    /// <remarks>
    /// Transport-independent counterpart to <see cref="SendMessageCommand"/> for the
    /// batch send path. A transport that registers a handler for this command performs a
    /// true bulk insert; transports without a handler fall back to the per-message loop in
    /// <c>SendMessages&lt;T&gt;</c>. The relational transports carry an optional
    /// caller-supplied transaction via the derived
    /// <c>RelationalSendMessageCommandBatch</c>.
    /// </remarks>
    public class SendMessageCommandBatch
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatch"/> class.
        /// </summary>
        /// <param name="messages">The messages to send, in caller order.</param>
        public SendMessageCommandBatch(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            Guard.NotNull(() => messages, messages);
            Messages = messages;
        }

        /// <summary>
        /// Gets the messages to send, in caller order. Generated ids are returned in this
        /// same order.
        /// </summary>
        public List<QueueMessage<IMessage, IAdditionalMessageData>> Messages { get; }
    }
}
