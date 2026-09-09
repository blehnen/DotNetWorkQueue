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
    /// <summary>
    /// Everything the synchronous and asynchronous receive handlers share, which is all of it except
    /// the one call that runs the Lua script.
    /// </summary>
    /// <remarks>
    /// This started as two near-identical classes differing in a single line. That was deliberate -
    /// the parsing, the poison detection, the three SetMessageAndHeaders calls and the expired-message
    /// branch are load-bearing, and a difference between two copies would surface on only one code
    /// path. Sharing one implementation is a stronger guarantee of the same property, and it means the
    /// existing tests for the synchronous path cover the asynchronous one too.
    ///
    /// The dequeue call keeps its own try/catch in each handler so that a transport failure still
    /// surfaces as ReceiveMessageException exactly as it did before.
    /// </remarks>
    internal abstract class AReceiveMessageQueryHandler
    {
        private readonly ICompositeSerialization _serializer;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly IRemoveMessage _removeMessage;
        private readonly RedisHeaders _redisHeaders;
        /// <summary>The dequeue script, used by the derived handler.</summary>
        protected readonly DequeueLua DequeueLua;
        /// <summary>Unix time, used by the derived handler.</summary>
        protected readonly IUnixTimeFactory UnixTimeFactory;
        private readonly IMessageFactory _messageFactory;

        /// <summary>Initializes a new instance of the <see cref="AReceiveMessageQueryHandler"/> class.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="removeMessage">Removes a message from the queue</param>
        /// <param name="redisHeaders">The redisHeaders.</param>
        /// <param name="dequeueLua">The dequeue.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        public AReceiveMessageQueryHandler(
            ICompositeSerialization serializer,
            IReceivedMessageFactory receivedMessageFactory,
            IRemoveMessage removeMessage,
            RedisHeaders redisHeaders,
            DequeueLua dequeueLua,
            IUnixTimeFactory unixTimeFactory,
            IMessageFactory messageFactory)
        {
            Guard.NotNull(serializer);
            Guard.NotNull(receivedMessageFactory);
            Guard.NotNull(removeMessage);
            Guard.NotNull(redisHeaders);
            Guard.NotNull(dequeueLua);
            Guard.NotNull(unixTimeFactory);

            _serializer = serializer;
            _receivedMessageFactory = receivedMessageFactory;
            _removeMessage = removeMessage;
            _redisHeaders = redisHeaders;
            DequeueLua = dequeueLua;
            UnixTimeFactory = unixTimeFactory;
            _messageFactory = messageFactory;
        }

        protected RedisMessage BuildMessage(ReceiveMessageQuery query, long unixTimestamp, RedisValue[] result)
        {
            byte[] message = null;
            byte[] headers = null;
            string messageId;
            var poisonMessage = false;
            RedisQueueCorrelationIdSerialized correlationId = null;
            try
            {

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
                    if (result[3].HasValue &&
                        result[3].TryParse(out long messageExpiration) &&
                        messageExpiration - unixTimestamp < 0)
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
