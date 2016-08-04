// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        public CommitMessage(
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand)
        {
            Guard.NotNull(() => deleteMessageCommand, deleteMessageCommand);
            _deleteMessageCommand = deleteMessageCommand;
        }
        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                _deleteMessageCommand.Handle(new DeleteMessageCommand((long)context.MessageId.Id.Value));
            }
        }
    }
}
