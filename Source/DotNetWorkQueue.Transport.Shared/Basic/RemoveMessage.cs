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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <summary>
    /// Removes a message from storage
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRemoveMessage" />
    public class RemoveMessage<T> : IRemoveMessage
    {
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand<T>, long> _deleteMessageCommandHandler;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessages{T}" /> class.
        /// </summary>
        /// <param name="deleteMessageCommandHandler">The delete message command handler.</param>
        public RemoveMessage(ICommandHandlerWithOutput<DeleteMessageCommand<T>, long> deleteMessageCommandHandler)
        {
            Guard.NotNull(() => deleteMessageCommandHandler, deleteMessageCommandHandler);
            _deleteMessageCommandHandler = deleteMessageCommandHandler;
        }
        #endregion

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id, RemoveMessageReason reason)
        {
            if (id != null && id.HasValue)
            {
                var result = _deleteMessageCommandHandler.Handle(new DeleteMessageCommand<T>((T)id.Id.Value));
                if (result > 0)
                    return RemoveMessageStatus.Removed;
            }

            return RemoveMessageStatus.NotFound;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context, RemoveMessageReason reason)
        {
            return Remove(context.MessageId, reason);
        }
    }
}
