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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Handles receiving a message that has failed to process
    /// </summary>
    internal class RedisQueueReceiveMessagesError : IReceiveMessagesError
    {
        private readonly ILogger _log;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<GetMetaDataQuery, RedisMetaData> _queryGetMetaData;
        private readonly IQueryHandlerAsync<GetMetaDataQuery, RedisMetaData> _queryGetMetaDataAsync;
        private readonly ICommandHandler<SaveMetaDataCommand> _saveMetaData;
        private readonly ICommandHandlerAsync<SaveMetaDataCommand> _saveMetaDataAsync;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<string>> _commandMoveRecord;
        private readonly ICommandHandlerAsync<MoveRecordToErrorQueueCommand<string>> _commandMoveRecordAsync;
        private readonly RedisHeaders _headers;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceiveMessagesError"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queryGetMetaData">The query get meta data.</param>
        /// <param name="queryGetMetaDataAsync">The query get meta data, asynchronous.</param>
        /// <param name="saveMetaData">The save meta data.</param>
        /// <param name="saveMetaDataAsync">The save meta data, asynchronous.</param>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="commandMoveRecordAsync">The command move record, asynchronous.</param>
        /// <param name="log">The log.</param>
        /// <param name="headers">The headers.</param>
        public RedisQueueReceiveMessagesError(
            QueueConsumerConfiguration configuration,
            IQueryHandler<GetMetaDataQuery, RedisMetaData> queryGetMetaData,
            IQueryHandlerAsync<GetMetaDataQuery, RedisMetaData> queryGetMetaDataAsync,
            ICommandHandler<SaveMetaDataCommand> saveMetaData,
            ICommandHandlerAsync<SaveMetaDataCommand> saveMetaDataAsync,
            ICommandHandler<MoveRecordToErrorQueueCommand<string>> commandMoveRecord,
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<string>> commandMoveRecordAsync,
            ILogger log,
            RedisHeaders headers)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(queryGetMetaData);
            Guard.NotNull(queryGetMetaDataAsync);
            Guard.NotNull(saveMetaData);
            Guard.NotNull(saveMetaDataAsync);
            Guard.NotNull(commandMoveRecord);
            Guard.NotNull(commandMoveRecordAsync);
            Guard.NotNull(log);
            Guard.NotNull(headers);

            _configuration = configuration;
            _queryGetMetaData = queryGetMetaData;
            _queryGetMetaDataAsync = queryGetMetaDataAsync;
            _saveMetaData = saveMetaData;
            _saveMetaDataAsync = saveMetaDataAsync;
            _commandMoveRecord = commandMoveRecord;
            _commandMoveRecordAsync = commandMoveRecordAsync;
            _log = log;
            _headers = headers;
        }

        #endregion

        /// <summary>
        /// Handles a message that has failed processing
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context,
            Exception exception)
        {
            //message failed to process
            if (!TryGetRetryInformation(context, exception, out var info, out var exceptionType))
                return ReceiveMessagesErrorResult.NoActionPossible;

            if (CanRetry(info, exceptionType))
            {
                //determine how many times this exception has been seen for this message
                var metadata = _queryGetMetaData.Handle(new GetMetaDataQuery((RedisQueueId)context.MessageId));
                if (CanCountAttempts(metadata))
                {
                    var retries = metadata.ErrorTracking.GetExceptionCount(exceptionType);
                    if (retries < info.MaxRetries)
                    {
                        CountAttempt(context, info, metadata, exceptionType, retries);
                        _saveMetaData.Handle(new SaveMetaDataCommand((RedisQueueId)context.MessageId, metadata));
                        return ReceiveMessagesErrorResult.Retry;
                    }
                }
            }

            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand<string>(exception, context.MessageId.Id.Value.ToString(), context));
            return MovedToErrorQueue(message, context, exception);
        }

        /// <summary>
        /// Handles a message that has failed processing
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public async Task<ReceiveMessagesErrorResult> MessageFailedProcessingAsync(IReceivedMessageInternal message, IMessageContext context,
            Exception exception)
        {
            //message failed to process
            if (!TryGetRetryInformation(context, exception, out var info, out var exceptionType))
                return ReceiveMessagesErrorResult.NoActionPossible;

            if (CanRetry(info, exceptionType))
            {
                //determine how many times this exception has been seen for this message
                var metadata = await _queryGetMetaDataAsync
                    .HandleAsync(new GetMetaDataQuery((RedisQueueId)context.MessageId)).ConfigureAwait(false);
                if (CanCountAttempts(metadata))
                {
                    var retries = metadata.ErrorTracking.GetExceptionCount(exceptionType);
                    if (retries < info.MaxRetries)
                    {
                        CountAttempt(context, info, metadata, exceptionType, retries);
                        await _saveMetaDataAsync
                            .HandleAsync(new SaveMetaDataCommand((RedisQueueId)context.MessageId, metadata))
                            .ConfigureAwait(false);
                        return ReceiveMessagesErrorResult.Retry;
                    }
                }
            }

            await _commandMoveRecordAsync.HandleAsync(
                new MoveRecordToErrorQueueCommand<string>(exception, context.MessageId.Id.Value.ToString(), context))
                .ConfigureAwait(false);
            return MovedToErrorQueue(message, context, exception);
        }

        /// <summary>
        /// Returns false when the message has no id, which is the only case in which nothing can be done.
        /// </summary>
        private bool TryGetRetryInformation(IMessageContext context, Exception exception, out IRetryInformation info, out string exceptionType)
        {
            info = null;
            exceptionType = null;
            if (context.MessageId == null || !context.MessageId.HasValue) return false;

            info = _configuration.TransportConfiguration.RetryDelayBehavior.GetRetryAmount(exception);
            if (info.ExceptionType != null)
            {
                exceptionType = info.ExceptionType.ToString();
            }
            return true;
        }

        /// <summary>
        /// An exception with no configured retry behaviour goes straight to the error queue; there is
        /// no point in counting attempts for it.
        /// </summary>
        private static bool CanRetry(IRetryInformation info, string exceptionType)
        {
            return !string.IsNullOrEmpty(exceptionType) && info.MaxRetries > 0;
        }

        /// <summary>
        /// A metadata hash is written when the message is sent and removed with the message, so this is
        /// only false if the two have already diverged. There is then nothing to count attempts in, so
        /// the message goes to the error queue rather than the error handler throwing.
        /// </summary>
        private static bool CanCountAttempts(RedisMetaData metadata)
        {
            return metadata?.ErrorTracking != null;
        }

        private void CountAttempt(IMessageContext context, IRetryInformation info, RedisMetaData metadata, string exceptionType, int retries)
        {
            //note zero based index - use the current count not count +1
            context.Set(_headers.IncreaseQueueDelay, new RedisQueueDelay(info.Times[retries]));
            metadata.ErrorTracking.IncrementExceptionCount(exceptionType);
        }

        private ReceiveMessagesErrorResult MovedToErrorQueue(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //we are done doing any processing - remove the messageID to block other actions
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
            _log.LogError(exception, "Message with ID {MessageId} has failed and has been moved to the error queue", message.MessageId);
            return ReceiveMessagesErrorResult.Error;
        }
    }
}

