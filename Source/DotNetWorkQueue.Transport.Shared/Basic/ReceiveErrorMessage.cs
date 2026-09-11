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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using QueueDelay = DotNetWorkQueue.Queue.QueueDelay;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class ReceiveErrorMessage<T> : AReceiveErrorMessage
    {
        #region Member Level Variables
        private readonly IQueryHandler<GetErrorRetryCountQuery<T>, int> _queryErrorRetryCount;
        private readonly IQueryHandlerAsync<GetErrorRetryCountQuery<T>, int> _queryErrorRetryCountAsync;
        private readonly ICommandHandler<SetErrorCountCommand<T>> _commandSetErrorCount;
        private readonly ICommandHandlerAsync<SetErrorCountCommand<T>> _commandSetErrorCountAsync;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<T>> _commandMoveRecord;
        private readonly ICommandHandlerAsync<MoveRecordToErrorQueueCommand<T>> _commandMoveRecordAsync;
        private readonly IIncreaseQueueDelay _headers;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveErrorMessage{T}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queryErrorRetryCount">The query error retry count.</param>
        /// <param name="queryErrorRetryCountAsync">The query error retry count, asynchronous.</param>
        /// <param name="commandSetErrorCount">The command set error count.</param>
        /// <param name="commandSetErrorCountAsync">The command set error count, asynchronous.</param>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="commandMoveRecordAsync">The command move record, asynchronous.</param>
        /// <param name="log">The log.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveErrorMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<GetErrorRetryCountQuery<T>, int> queryErrorRetryCount,
            IQueryHandlerAsync<GetErrorRetryCountQuery<T>, int> queryErrorRetryCountAsync,
            ICommandHandler<SetErrorCountCommand<T>> commandSetErrorCount,
            ICommandHandlerAsync<SetErrorCountCommand<T>> commandSetErrorCountAsync,
            ICommandHandler<MoveRecordToErrorQueueCommand<T>> commandMoveRecord,
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<T>> commandMoveRecordAsync,
            ILogger log,
            IIncreaseQueueDelay headers)
            : base(configuration, log)
        {
            Guard.NotNull(queryErrorRetryCount);
            Guard.NotNull(queryErrorRetryCountAsync);
            Guard.NotNull(commandSetErrorCount);
            Guard.NotNull(commandSetErrorCountAsync);
            Guard.NotNull(commandMoveRecord);
            Guard.NotNull(commandMoveRecordAsync);

            _queryErrorRetryCount = queryErrorRetryCount;
            _queryErrorRetryCountAsync = queryErrorRetryCountAsync;
            _commandSetErrorCount = commandSetErrorCount;
            _commandSetErrorCountAsync = commandSetErrorCountAsync;
            _commandMoveRecord = commandMoveRecord;
            _commandMoveRecordAsync = commandMoveRecordAsync;
            _headers = headers;
        }
        #endregion

        #region IReceiveMessagesError
        /// <inheritdoc />
        public override ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //message failed to process
            if (!TryGetRetryInformation(context, exception, out var info, out var exceptionType))
                return ReceiveMessagesErrorResult.NoActionPossible;

            if (CanRetry(info, exceptionType))
            {
                //determine how many times this exception has been seen for this message
                var retries = _queryErrorRetryCount.Handle(
                    new GetErrorRetryCountQuery<T>(exceptionType, MessageId(context)));
                if (retries < info.MaxRetries)
                {
                    DelayNextAttempt(context, info, retries);
                    _commandSetErrorCount.Handle(
                        new SetErrorCountCommand<T>(exceptionType, MessageId(context)));
                    return ReceiveMessagesErrorResult.Retry;
                }
            }

            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand<T>(exception, MessageId(context), context));
            return MovedToErrorQueue(message, context, exception);
        }

        /// <inheritdoc />
        public override async Task<ReceiveMessagesErrorResult> MessageFailedProcessingAsync(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //message failed to process
            if (!TryGetRetryInformation(context, exception, out var info, out var exceptionType))
                return ReceiveMessagesErrorResult.NoActionPossible;

            if (CanRetry(info, exceptionType))
            {
                //determine how many times this exception has been seen for this message
                var retries = await _queryErrorRetryCountAsync.HandleAsync(
                    new GetErrorRetryCountQuery<T>(exceptionType, MessageId(context))).ConfigureAwait(false);
                if (retries < info.MaxRetries)
                {
                    DelayNextAttempt(context, info, retries);
                    await _commandSetErrorCountAsync.HandleAsync(
                        new SetErrorCountCommand<T>(exceptionType, MessageId(context))).ConfigureAwait(false);
                    return ReceiveMessagesErrorResult.Retry;
                }
            }

            await _commandMoveRecordAsync.HandleAsync(
                new MoveRecordToErrorQueueCommand<T>(exception, MessageId(context), context)).ConfigureAwait(false);
            return MovedToErrorQueue(message, context, exception);
        }
        #endregion

        #region Shared by both paths
        private void DelayNextAttempt(IMessageContext context, IRetryInformation info, int retries)
        {
            //note zero based index - use the current count not count +1
            context.Set(_headers.QueueDelay, new QueueDelay(info.Times[retries]));
        }

        private static T MessageId(IMessageContext context)
        {
            return (T)context.MessageId.Id.Value;
        }
        #endregion
    }
}
