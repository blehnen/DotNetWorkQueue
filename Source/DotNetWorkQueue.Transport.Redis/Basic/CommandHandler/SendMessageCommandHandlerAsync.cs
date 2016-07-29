// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandlerAsync : ICommandHandlerWithOutput<SendMessageCommand, Task<string>>
    {
        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly EnqueueLua _enqueueLua;
        private readonly EnqueueDelayedLua _enqueueDelayedLua;
        private readonly EnqueueExpirationLua _enqueueExpirationLua;
        private readonly EnqueueDelayedAndExpirationLua _enqueueDelayedAndExpirationLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IGetMessageIdFactory _messageIdFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="enqueueLua">The enqueue.</param>
        /// <param name="enqueueDelayedLua">The enqueue delayed.</param>
        /// <param name="enqueueExpirationLua">The enqueue expiration.</param>
        /// <param name="enqueueDelayedAndExpirationLua">The enqueue delayed and expiration.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageIdFactory">The message identifier factory.</param>
        public SendMessageCommandHandlerAsync(
            ICompositeSerialization serializer,
            IHeaders headers,
            EnqueueLua enqueueLua,
            EnqueueDelayedLua enqueueDelayedLua,
            EnqueueExpirationLua enqueueExpirationLua,
            EnqueueDelayedAndExpirationLua enqueueDelayedAndExpirationLua,
            IUnixTimeFactory unixTimeFactory,
            IGetMessageIdFactory messageIdFactory)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => enqueueLua, enqueueLua);
            Guard.NotNull(() => enqueueDelayedLua, enqueueDelayedLua);
            Guard.NotNull(() => enqueueExpirationLua, enqueueExpirationLua);
            Guard.NotNull(() => enqueueDelayedAndExpirationLua, enqueueDelayedAndExpirationLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => messageIdFactory, messageIdFactory);

            _serializer = serializer;
            _headers = headers;
            _enqueueLua = enqueueLua;
            _messageIdFactory = messageIdFactory;

            _enqueueDelayedLua = enqueueDelayedLua;
            _enqueueDelayedAndExpirationLua = enqueueDelayedAndExpirationLua;
            _unixTimeFactory = unixTimeFactory;
            _enqueueExpirationLua = enqueueExpirationLua;
        }

        /// <summary>
        /// Handles the command.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <returns></returns>
        public async Task<string> Handle(SendMessageCommand commandSend)
        {
            TimeSpan? delay = null;
            //there are three possible locations for a message expiration. The user data and the header / internal headers
            //grab it from the internal header
            TimeSpan? expiration =
                commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcTimeout).Timeout;
            //if the header value is zero, check the message expiration
            if (expiration == TimeSpan.Zero)
            {
                //try the message header
                expiration = commandSend.MessageToSend.GetHeader(_headers.StandardHeaders.RpcTimeout).Timeout;
            }

            //if the header value is zero, check the message expiration
            if (expiration == TimeSpan.Zero && commandSend.MessageData.GetExpiration().HasValue)
            {
                // ReSharper disable once PossibleInvalidOperationException
                expiration = commandSend.MessageData.GetExpiration().Value;
            }

            //treat a zero time as null
            if (expiration == TimeSpan.Zero)
            {
                expiration = null;
            }

            if (commandSend.MessageData.GetDelay().HasValue)
            {
                delay = commandSend.MessageData.GetDelay();
            }

            //determine which path to send the message on
            if (delay.HasValue && expiration.HasValue)
            {
                return
                    await
                        SendDelayAndExpirationMessage(commandSend,
                            _unixTimeFactory.Create().GetAddDifferenceMilliseconds(delay.Value),
                            _unixTimeFactory.Create().GetAddDifferenceMilliseconds(expiration.Value))
                            .ConfigureAwait(false);
            }
            if (delay.HasValue)
            {
                return
                    await
                        SendDelayMessage(commandSend,
                            _unixTimeFactory.Create().GetAddDifferenceMilliseconds(delay.Value)).ConfigureAwait(false);
            }
            if (expiration.HasValue)
            {
                return
                    await
                        SendExpirationMessage(commandSend,
                            _unixTimeFactory.Create().GetAddDifferenceMilliseconds(expiration.Value))
                            .ConfigureAwait(false);
            }
            return await SendStandardMessage(commandSend).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the standard message.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to enqueue a record. The LUA enqueue script returned null</exception>
        private async Task<string> SendStandardMessage(SendMessageCommand commandSend)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var rpc = false;
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
                rpc = true;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());
            var serialized = _serializer.Serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var jobName = GetJobName(commandSend);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = GetJobTime("JobScheduledTime", commandSend);
                eventTime = GetJobTime("JobEventTime", commandSend);
            }

            var result = await _enqueueLua.ExecuteAsync(messageId,
                serialized.Output, _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _serializer.InternalSerializer.ConvertToBytes(meta), rpc, jobName, scheduledTime, eventTime).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new DotNetWorkQueueException("Failed to enqueue a record. The LUA enqueue script returned null");
            }
            messageId = result;
            return messageId;
        }

        /// <summary>
        /// Sends a delayed message.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <param name="delayTime">The delay time.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to enqueue a record. The LUA enqueue script returned null</exception>
        private async Task<string> SendDelayMessage(SendMessageCommand commandSend, long delayTime)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());
            var serialized = _serializer.Serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = await _enqueueDelayedLua.ExecuteAsync(messageId,
                serialized.Output, _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _serializer.InternalSerializer.ConvertToBytes(meta), delayTime).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new DotNetWorkQueueException("Failed to enqueue a record. The LUA enqueue script returned null");
            }
            messageId = result;
            return messageId;
        }

        /// <summary>
        /// Sends a delayed and expired message.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <param name="delayTime">The delay time.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to enqueue a record. The LUA enqueue script returned null</exception>
        private async Task<string> SendDelayAndExpirationMessage(SendMessageCommand commandSend, long delayTime,
            long expireTime)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());

            var serialized = _serializer.Serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = await _enqueueDelayedAndExpirationLua.ExecuteAsync(messageId,
                serialized.Output, _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _serializer.InternalSerializer.ConvertToBytes(meta), delayTime,
                expireTime).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new DotNetWorkQueueException("Failed to enqueue a record. The LUA enqueue script returned null");
            }
            messageId = result;
            return messageId;
        }

        /// <summary>
        /// Sends an expired message.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <param name="expireTime">The expire time.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to enqueue a record. The LUA enqueue script returned null</exception>
        private async Task<string> SendExpirationMessage(SendMessageCommand commandSend, long expireTime)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var rpc = false;
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
                rpc = true;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());
            var serialized = _serializer.Serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = await _enqueueExpirationLua.ExecuteAsync(messageId,
                serialized.Output, _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _serializer.InternalSerializer.ConvertToBytes(meta), expireTime, rpc).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new DotNetWorkQueueException("Failed to enqueue a record. The LUA enqueue script returned null");
            }
            messageId = result;
            return messageId;
        }

        private string GetJobName(SendMessageCommand commandSend)
        {
            foreach (var meta in commandSend.MessageData.AdditionalMetaData)
            {
                if (meta.Name == "JobName" && meta.Value is string)
                {
                    return (string)meta.Value;
                }
            }
            return string.Empty;
        }
        private DateTimeOffset GetJobTime(string field, SendMessageCommand commandSend)
        {
            foreach (var meta in commandSend.MessageData.AdditionalMetaData)
            {
                if (meta.Name == field && meta.Value is DateTimeOffset)
                {
                    return (DateTimeOffset)meta.Value;
                }
            }
            return DateTimeOffset.MinValue;
        }
    }

}
