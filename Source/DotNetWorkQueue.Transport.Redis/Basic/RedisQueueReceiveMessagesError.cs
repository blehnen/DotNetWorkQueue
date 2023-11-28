﻿// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;

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
        private readonly ICommandHandler<SaveMetaDataCommand> _saveMetaData;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand<string>> _commandMoveRecord;
        private readonly RedisHeaders _headers;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceiveMessagesError"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queryGetMetaData">The query get meta data.</param>
        /// <param name="saveMetaData">The save meta data.</param>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="log">The log.</param>
        /// <param name="headers">The headers.</param>
        public RedisQueueReceiveMessagesError(
            QueueConsumerConfiguration configuration,
            IQueryHandler<GetMetaDataQuery, RedisMetaData> queryGetMetaData,
            ICommandHandler<SaveMetaDataCommand> saveMetaData,
            ICommandHandler<MoveRecordToErrorQueueCommand<string>> commandMoveRecord,
            ILogger log,
            RedisHeaders headers)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => queryGetMetaData, queryGetMetaData);
            Guard.NotNull(() => saveMetaData, saveMetaData);
            Guard.NotNull(() => commandMoveRecord, commandMoveRecord);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => headers, headers);

            _configuration = configuration;
            _queryGetMetaData = queryGetMetaData;
            _saveMetaData = saveMetaData;
            _commandMoveRecord = commandMoveRecord;
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
                var metadata = _queryGetMetaData.Handle(new GetMetaDataQuery((RedisQueueId)context.MessageId));
                var retries = metadata.ErrorTracking.GetExceptionCount(exceptionType);
                if (retries >= info.MaxRetries)
                {
                    bSendErrorQueue = true;
                }
                else
                {
                    context.Set(_headers.IncreaseQueueDelay, new RedisQueueDelay(info.Times[retries]));
                    metadata.ErrorTracking.IncrementExceptionCount(exceptionType);
                    _saveMetaData.Handle(new SaveMetaDataCommand((RedisQueueId)context.MessageId, metadata));
                }
            }

            if (!bSendErrorQueue) return ReceiveMessagesErrorResult.Retry;

            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand<string>(exception, context.MessageId.Id.Value.ToString(), context));
            //we are done doing any processing - remove the messageID to block other actions
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
            _log.LogError($"Message with ID {message.MessageId} has failed and has been moved to the error queue{System.Environment.NewLine}{exception}");
            return ReceiveMessagesErrorResult.Error;
        }
    }
}

