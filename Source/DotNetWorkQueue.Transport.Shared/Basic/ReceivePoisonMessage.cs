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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class ReceivePoisonMessage<T> : IReceivePoisonMessage
    {
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<T>> _commandMoveRecord;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivePoisonMessage{T}"/> class.
        /// </summary>
        /// <param name="commandMoveRecord">The command move record.</param>
        public ReceivePoisonMessage(ICommandHandler<MoveRecordToErrorQueueCommand<T>> commandMoveRecord)
        {
            Guard.NotNull(() => commandMoveRecord, commandMoveRecord);
            _commandMoveRecord = commandMoveRecord;
        }
        /// <inheritdoc />
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            Guard.NotNull(() => context, context);
            Guard.NotNull(() => exception, exception);

            if (context.MessageId == null || !context.MessageId.HasValue) return;

            var messageId = (T)context.MessageId.Id.Value;
            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand<T>(exception, messageId, context));
            context.SetMessageAndHeaders(null, context.Headers);
        }
    }
}
