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
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery, RedisMessage>
    {
        private readonly ICompositeSerialization _serializer;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly IRemoveMessage _removeMessage;
        private readonly RedisHeaders _redisHeaders;
        private readonly DequeueLua _dequeueLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IMessageFactory _messageFactory;

        /// <summary>Initializes a new instance of the <see cref="ReceiveMessageQueryHandler"/> class.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="removeMessage">Removes a message from the queue</param>
        /// <param name="redisHeaders">The redisHeaders.</param>
        /// <param name="dequeueLua">The dequeue.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        public ReceiveMessageQueryHandler(
            ICompositeSerialization serializer,
            IReceivedMessageFactory receivedMessageFactory,
            IRemoveMessage removeMessage,
            RedisHeaders redisHeaders,
            DequeueLua dequeueLua,
            IUnixTimeFactory unixTimeFactory,
            IMessageFactory messageFactory)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => receivedMessageFactory, receivedMessageFactory);
            Guard.NotNull(() => removeMessage, removeMessage);
            Guard.NotNull(() => redisHeaders, redisHeaders);
            Guard.NotNull(() => dequeueLua, dequeueLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);

            _serializer = serializer;
            _receivedMessageFactory = receivedMessageFactory;
            _removeMessage = removeMessage;
            _redisHeaders = redisHeaders;
            _dequeueLua = dequeueLua;
            _unixTimeFactory = unixTimeFactory;
            _messageFactory = messageFactory;
        }

        /// <inheritdoc />
        public RedisMessage Handle(ReceiveMessageQuery query)
        {
            byte[] message = null;
            byte[] headers = null;
            string messageId;
            var poisonMessage = false;
            RedisQueueCorrelationIdSerialized correlationId = null;
            try
            {
                var unixTimestamp = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
                RedisValue[] result = _dequeueLua.Execute(unixTimestamp);

                if (result == null || result.Length == 1 && !result[0].HasValue || !result[0].HasValue)
                {
                    return null;
                }

                if (!result[1].HasValue)
                {
                    //at this point, the record has been de-queued, but it can't be processed.
                    poisonMessage = true;
                }

                messageId = result[0];
                var id = new RedisQueueId(messageId);
                query.MessageContext.SetMessageAndHeaders(id, null, null);
                if (!poisonMessage)
                {
                    message = result[1];
                    headers = result[2];
                    if (result[3].HasValue)
                    {
                        if (result[3].TryParse(out long messageExpiration))
                        {
                            if (messageExpiration - unixTimestamp < 0)
                            {
                                //message has expired
                                var allHeaders = _serializer.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headers);
                                correlationId = (RedisQueueCorrelationIdSerialized)allHeaders[_redisHeaders.CorrelationId.Name];
                                query.MessageContext.SetMessageAndHeaders(id, new RedisQueueCorrelationId(correlationId.Id), new ReadOnlyDictionary<string, object>(allHeaders));
                                _removeMessage.Remove(query.MessageContext, RemoveMessageReason.Expired);
                                return new RedisMessage(messageId, null, true);
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                throw new ReceiveMessageException("Failed to dequeue a message", error);
            }

            if (poisonMessage)
            {
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a message de-queued from Redis; a messageId was returned, but the LUA script returned a null message. The message payload has most likely been lost.", null,
                    new RedisQueueId(messageId), new RedisQueueCorrelationId(Guid.Empty), null,
                    null, null);
            }

            try
            {
                var allHeaders = _serializer.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headers);
                correlationId = (RedisQueueCorrelationIdSerialized)allHeaders[_redisHeaders.CorrelationId.Name];
                var messageGraph = (MessageInterceptorsGraph)allHeaders[_redisHeaders.Headers.StandardHeaders.MessageInterceptorGraph.Name];
                var messageData = _serializer.Serializer.BytesToMessage<MessageBody>(message, messageGraph, allHeaders);

                var newMessage = _messageFactory.Create(messageData.Body, allHeaders);
                query.MessageContext.SetMessageAndHeaders(query.MessageContext.MessageId, new RedisQueueCorrelationId(correlationId.Id), new ReadOnlyDictionary<string, object>(allHeaders));

                return new RedisMessage(
                        messageId,
                        _receivedMessageFactory.Create(
                        newMessage,
                        new RedisQueueId(messageId),
                        new RedisQueueCorrelationId(correlationId.Id)), false);
            }
            catch (Exception error)
            {
                var allHeaders = _serializer.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headers);

                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a message de-queued from redis", error,
                    new RedisQueueId(messageId), new RedisQueueCorrelationId(correlationId), new ReadOnlyDictionary<string, object>(allHeaders),
                    message, headers);

            }
        }
    }
}
