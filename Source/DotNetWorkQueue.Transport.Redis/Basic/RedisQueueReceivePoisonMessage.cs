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
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
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
        private readonly ICommandHandlerAsync<MoveRecordToErrorQueueCommand<string>> _commandMoveRecordAsync;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceivePoisonMessage"/> class.
        /// </summary>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="commandMoveRecordAsync">The command move record, for the asynchronous consumer.</param>
        public RedisQueueReceivePoisonMessage(ICommandHandler<MoveRecordToErrorQueueCommand<string>> commandMoveRecord,
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<string>> commandMoveRecordAsync)
        {
            _commandMoveRecord = commandMoveRecord;
            _commandMoveRecordAsync = commandMoveRecordAsync;
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
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
        }

        /// <inheritdoc />
        public async Task HandleAsync(IMessageContext context, PoisonMessageException exception)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                await _commandMoveRecordAsync.HandleAsync(
                    new MoveRecordToErrorQueueCommand<string>(exception, context.MessageId.Id.Value.ToString(), context))
                    .ConfigureAwait(false);
            }
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
        }
    }
}
