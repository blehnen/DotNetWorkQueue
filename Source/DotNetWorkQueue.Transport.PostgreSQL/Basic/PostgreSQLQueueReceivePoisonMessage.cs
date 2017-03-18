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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Handles moving poison messages to the error table
    /// </summary>
    internal class PostgreSqlQueueReceivePoisonMessage : IReceivePoisonMessage
    {
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand> _commandMoveRecord;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlQueueReceivePoisonMessage"/> class.
        /// </summary>
        /// <param name="commandMoveRecord">The command move record.</param>
        public PostgreSqlQueueReceivePoisonMessage(ICommandHandler<MoveRecordToErrorQueueCommand> commandMoveRecord)
        {
            Guard.NotNull(() => commandMoveRecord, commandMoveRecord); 
            _commandMoveRecord = commandMoveRecord;
        }
        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occurred during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            Guard.NotNull(() => context, context);
            Guard.NotNull(() => exception, exception);

            if (context.MessageId == null || !context.MessageId.HasValue) return;

            var messageId = (long)context.MessageId.Id.Value;
            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand(exception, messageId, context));
            context.MessageId = null;
        }
    }
}
