// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, string>
    {
        private readonly ASerializer _serializer;
        private readonly IHeaders _headers;
        private readonly EnqueueLua _enqueueLua;
        private readonly EnqueueDelayedLua _enqueueDelayedLua;
        private readonly EnqueueExpirationLua _enqueueExpirationLua;
        private readonly EnqueueDelayedAndExpirationLua _enqueueDelayedAndExpirationLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IGetMessageIdFactory _messageIdFactory;
        private readonly IInternalSerializer _internalSerializer;

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
        /// <param name="internalSerializer">The internal serializer.</param>
        public SendMessageCommandHandler(
            ASerializer serializer,
            IHeaders headers,
            EnqueueLua enqueueLua,
            EnqueueDelayedLua enqueueDelayedLua,
            EnqueueExpirationLua enqueueExpirationLua,
            EnqueueDelayedAndExpirationLua enqueueDelayedAndExpirationLua,
            IUnixTimeFactory unixTimeFactory,
            IGetMessageIdFactory messageIdFactory, 
            IInternalSerializer internalSerializer)
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
            _internalSerializer = internalSerializer;

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
        public string Handle(SendMessageCommand commandSend)
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
                return SendDelayAndExpirationMessage(commandSend,
                    _unixTimeFactory.Create().GetAddDifferenceMilliseconds(delay.Value),
                    _unixTimeFactory.Create().GetAddDifferenceMilliseconds(expiration.Value));
            }
            if (delay.HasValue)
            {
                return SendDelayMessage(commandSend, _unixTimeFactory.Create().GetAddDifferenceMilliseconds(delay.Value));
            }
            if (expiration.HasValue)
            {
                return SendExpirationMessage(commandSend,
                    _unixTimeFactory.Create().GetAddDifferenceMilliseconds(expiration.Value));
            }
            return SendStandardMessage(commandSend);
        }

        /// <summary>
        /// Sends the standard message.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">Failed to enqueue a record. The LUA enqueue script returned null</exception>
        private string SendStandardMessage(SendMessageCommand commandSend)
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

            var serialized = _serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = _enqueueLua.Execute(messageId,
                serialized.Output, _internalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers),_internalSerializer.ConvertToBytes(meta), rpc);

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
        private string SendDelayMessage(SendMessageCommand commandSend, long delayTime)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());

            var serialized = _serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = _enqueueDelayedLua.Execute(messageId,
                serialized.Output, _internalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _internalSerializer.ConvertToBytes(meta), delayTime);

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
        private string SendDelayAndExpirationMessage(SendMessageCommand commandSend, long delayTime, long expireTime)
        {
            var id = commandSend.MessageToSend.GetInternalHeader(_headers.StandardHeaders.RpcResponseId);
            var messageId = _messageIdFactory.Create().Create().ToString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                messageId = id;
            }
            var meta = new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds());

            var serialized = _serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = _enqueueDelayedAndExpirationLua.Execute(messageId,
                serialized.Output, _internalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _internalSerializer.ConvertToBytes(meta), delayTime,
                expireTime);

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
        private string SendExpirationMessage(SendMessageCommand commandSend, long expireTime)
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

            var serialized = _serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });
            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

            var result = _enqueueExpirationLua.Execute(messageId,
                serialized.Output, _internalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers), _internalSerializer.ConvertToBytes(meta), expireTime, rpc);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new DotNetWorkQueueException("Failed to enqueue a record. The LUA enqueue script returned null");
            }
            messageId = result;
            return messageId;
        }
    }
}
