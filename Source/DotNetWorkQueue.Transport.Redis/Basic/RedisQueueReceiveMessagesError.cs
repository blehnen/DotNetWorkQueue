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
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Handles receiving a message that has errored
    /// </summary>
    internal class RedisQueueReceiveMessagesError : IReceiveMessagesError
    {
        private readonly ILog _log;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<GetMetaDataQuery, RedisMetaData> _queryGetMetaData;
        private readonly ICommandHandler<SaveMetaDataCommand> _saveMetaData;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand> _commandMoveRecord;
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
            ICommandHandler<MoveRecordToErrorQueueCommand> commandMoveRecord,
            ILogFactory log,
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
            _log = log.Create();
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
                new MoveRecordToErrorQueueCommand((RedisQueueId)context.MessageId));
            //we are done doing any processing - remove the messageID to block other actions
            context.MessageId = null;
            _log.ErrorException("Message with ID {0} has failed and has been moved to the error queue", exception,
                message.MesssageId);
            return ReceiveMessagesErrorResult.Error;
        }
    }
}

