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

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Command
{
    /// <summary>
    /// Deletes a message, when it's being held by a transaction
    /// </summary>
    public class DeleteTransactionalMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommand" /> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="context">The context.</param>
        public DeleteTransactionalMessageCommand(long queueId, IMessageContext context)
        {
            Guard.NotNull(() => context, context);
            QueueId = queueId;
            MessageContext = context;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public long QueueId { get; set; }

        /// <summary>
        /// Gets the message context.
        /// </summary>
        /// <value>
        /// The message context.
        /// </value>
        public IMessageContext MessageContext { get; private set; }
    }
}
