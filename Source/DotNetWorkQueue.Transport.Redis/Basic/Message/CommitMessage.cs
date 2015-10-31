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

using DotNetWorkQueue.Transport.Redis.Basic.Command;
namespace DotNetWorkQueue.Transport.Redis.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly ICommandHandlerWithOutput<CommitMessageCommand, bool> _command;
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="command">The command.</param>
        public CommitMessage(ICommandHandlerWithOutput<CommitMessageCommand, bool> command)
        {
            Guard.NotNull(() => command, command);
            _command = command;
        }

        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return;
            _command.Handle(new CommitMessageCommand((RedisQueueId) context.MessageId));
        }
    }
}
