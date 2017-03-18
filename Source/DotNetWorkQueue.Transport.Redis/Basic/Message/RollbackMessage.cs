// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Message
{
    /// <summary>
    /// Rolls back a message by moving it from the working queue into another queue
    /// </summary>
    internal class RollbackMessage
    {
        private readonly RedisHeaders _headers;
        private readonly ICommandHandler<RollbackMessageCommand> _command;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessage" /> class.
        /// </summary>
        /// <param name="command">The command handler factory.</param>
        /// <param name="headers">The headers.</param>
        public RollbackMessage(ICommandHandler<RollbackMessageCommand> command, RedisHeaders headers)
        {
            Guard.NotNull(() => command, command);
            Guard.NotNull(() => headers, headers);

            _command = command;
            _headers = headers;
        }

        /// <summary>
        /// Rollback the specified message via the message context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Rollback(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return;

            var increaseDelay = context.Get(_headers.IncreaseQueueDelay).IncreaseDelay;
            _command.Handle(new RollbackMessageCommand((RedisQueueId)context.MessageId, increaseDelay));
            context.MessageId = null; //this message should not have any more actions performed on it
        }
    }
}
