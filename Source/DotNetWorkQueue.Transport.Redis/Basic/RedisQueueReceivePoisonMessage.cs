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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Handles receiving a poison message - a message that cannot be deserialized
    /// </summary>
    internal class RedisQueueReceivePoisonMessage : IReceivePoisonMessage
    {
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<string>> _commandMoveRecord;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceivePoisonMessage"/> class.
        /// </summary>
        /// <param name="commandMoveRecord">The command move record.</param>
        public RedisQueueReceivePoisonMessage(ICommandHandler<MoveRecordToErrorQueueCommand<string>> commandMoveRecord)
        {
            _commandMoveRecord = commandMoveRecord;
        }

        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occurred during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                _commandMoveRecord.Handle(new MoveRecordToErrorQueueCommand<string>(exception, context.MessageId.Id.Value.ToString(), context));
            }
            context.SetMessageAndHeaders(null, context.Headers);
        }
    }
}
