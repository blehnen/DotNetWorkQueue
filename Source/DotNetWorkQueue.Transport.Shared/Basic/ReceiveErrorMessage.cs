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
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using QueueDelay = DotNetWorkQueue.Queue.QueueDelay;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class ReceiveErrorMessage<T> : IReceiveMessagesError
    {
        #region Member Level Variables
        private readonly ILogger _log;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<GetErrorRetryCountQuery<T>, int> _queryErrorRetryCount;
        private readonly ICommandHandler<SetErrorCountCommand<T>> _commandSetErrorCount;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<T>> _commandMoveRecord;
        private readonly IIncreaseQueueDelay _headers;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveErrorMessage{T}" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queryErrorRetryCount">The query error retry count.</param>
        /// <param name="commandSetErrorCount">The command set error count.</param>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="log">The log.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveErrorMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<GetErrorRetryCountQuery<T>, int> queryErrorRetryCount, ICommandHandler<SetErrorCountCommand<T>> commandSetErrorCount,
            ICommandHandler<MoveRecordToErrorQueueCommand<T>> commandMoveRecord,
            ILogger log,
            IIncreaseQueueDelay headers)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => queryErrorRetryCount, queryErrorRetryCount);
            Guard.NotNull(() => commandSetErrorCount, commandSetErrorCount);
            Guard.NotNull(() => commandMoveRecord, commandMoveRecord);
            Guard.NotNull(() => log, log);

            _configuration = configuration;
            _queryErrorRetryCount = queryErrorRetryCount;
            _commandSetErrorCount = commandSetErrorCount;
            _commandMoveRecord = commandMoveRecord;
            _log = log;
            _headers = headers;
        }
        #endregion

        #region IReceiveMessagesError
        /// <inheritdoc />
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //message failed to process
            if (context.MessageId == null || !context.MessageId.HasValue) return ReceiveMessagesErrorResult.NoActionPossible;

            var info =
                _configuration.TransportConfiguration.RetryDelayBehavior.GetRetryAmount(exception);
            string exceptionType = null;
            if (info.ExceptionType != null)
            {
                exceptionType = info.ExceptionType.ToString();
            }

            var bSendErrorQueue = false;
            if (string.IsNullOrEmpty(exceptionType) || info.MaxRetries <= 0)
            {
                bSendErrorQueue = true;
            }
            else
            {
                //determine how many times this exception has been seen for this message
                var retries =
                    _queryErrorRetryCount.Handle(
                        new GetErrorRetryCountQuery<T>(exceptionType,
                            (T) context.MessageId.Id.Value));
                if (retries >= info.MaxRetries)
                {
                    bSendErrorQueue = true;
                }
                else
                {
                    context.Set(_headers.QueueDelay, new QueueDelay(info.Times[retries]));
                    //note zero based index - use the current count not count +1
                    _commandSetErrorCount.Handle(
                        new SetErrorCountCommand<T>(
                            exceptionType, (T) context.MessageId.Id.Value));
                }
            }

            if (!bSendErrorQueue) return ReceiveMessagesErrorResult.Retry;

            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand<T>(exception, (T)context.MessageId.Id.Value, context));
            //we are done doing any processing - remove the messageID to block other actions
            context.SetMessageAndHeaders(null, context.Headers);
            _log.LogError($"Message with ID {message.MessageId} has failed and has been moved to the error queue{System.Environment.NewLine}{exception}");
            return ReceiveMessagesErrorResult.Error;
        }
        #endregion
    }
}
