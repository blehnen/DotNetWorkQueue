// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic.Command;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Removes a message from storage
    /// </summary>
    public class RemoveMessage : IRemoveMessage
    {
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, bool> _deleteMessage;

        /// <summary>Initializes a new instance of the <see cref="RemoveMessage"/> class.</summary>
        /// <param name="deleteMessage">The delete message.</param>
        public RemoveMessage(ICommandHandlerWithOutput<DeleteMessageCommand, bool> deleteMessage)
        {
            _deleteMessage = deleteMessage;
        }
        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id)
        {
            if (id == null || !id.HasValue)
                return RemoveMessageStatus.NotFound;

            var result =_deleteMessage.Handle(new DeleteMessageCommand((RedisQueueId)id));
            return result ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context)
        {
            return Remove(context.MessageId);
        }
    }
}
