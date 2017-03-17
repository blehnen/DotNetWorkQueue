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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Shared logic for sending a batch of messages
    /// </summary>
    internal static class BatchMessageShared
    {
        /// <summary>
        /// Creates the messages to send.
        /// </summary>
        /// <param name="redisHeaders">The redis headers.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="meta">The meta data, already serialized.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageIdFactory">The message identifier factory.</param>
        /// <param name="serializer">The composite serializer.</param>
        /// <returns></returns>
        public static List<EnqueueBatchLua.MessageToSend> CreateMessagesToSend(RedisHeaders redisHeaders, 
            IReadOnlyCollection<QueueMessage<IMessage, IAdditionalMessageData>> messages,
            byte[] meta, 
            IUnixTimeFactory unixTimeFactory,
            IGetMessageIdFactory messageIdFactory,
            ICompositeSerialization serializer)
        {
            var messagesToSend = new ConcurrentBag<EnqueueBatchLua.MessageToSend>();
            Parallel.ForEach(messages, m =>
            {
                //the correlation ID must be saved as a message header
                m.Message.SetHeader(redisHeaders.CorelationId, new RedisQueueCorrelationIdSerialized((Guid)m.MessageData.CorrelationId.Id.Value));

                //check for delay and expiration
                long unixTimeStampDelay = 0;
                long unixTimeStampExpiration = 0;
                if (m.MessageData.GetDelay().HasValue)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    unixTimeStampDelay = unixTimeFactory.Create().GetAddDifferenceMilliseconds(m.MessageData.GetDelay().Value);
                }
                // ReSharper disable once PossibleInvalidOperationException
                if (m.MessageData.GetExpiration().HasValue && m.MessageData.GetExpiration().Value != TimeSpan.Zero)
                {
                    var unixTime = unixTimeFactory.Create();
                    var timeSpan = m.MessageData.GetExpiration();
                    if (timeSpan != null)
                    {
                        var timespan = timeSpan.Value;
                        unixTimeStampExpiration = unixTime.GetAddDifferenceMilliseconds(timespan);
                    }
                }

                var serialized = serializer.Serializer.MessageToBytes(new MessageBody { Body = m.Message.Body });
                m.Message.SetHeader(redisHeaders.Headers.StandardHeaders.MessageInterceptorGraph, serialized.Graph);

                messagesToSend.Add(new EnqueueBatchLua.MessageToSend()
                {
                    Message = serialized.Output,
                    Headers = serializer.InternalSerializer.ConvertToBytes(m.Message.Headers),
                    MessageId = messageIdFactory.Create().Create().ToString(),
                    MetaData = meta,
                    CorrelationId = m.MessageData.CorrelationId.ToString(),
                    TimeStamp = unixTimeStampDelay,
                    ExpireTimeStamp = unixTimeStampExpiration,
                    Route = m.MessageData.Route
                });
            });
            return messagesToSend.ToList();
        }

        /// <summary>
        /// Processes the sent messages.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="messageCount">The message count.</param>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException">$Failed to enqueue record(s). The LUA enqueue script returned the wrong count of items {result.Count}, expected {messageCount * 2}</exception>
        public static List<QueueOutputMessage> ProcessSentMessages(List<string> result, int messageCount, ISentMessageFactory sentMessageFactory)
        {
            var rc = new List<QueueOutputMessage>(messageCount);
            if (result.Count != messageCount * 2) //we get back 2 id's for each request - return count should be double
            {
                throw new DotNetWorkQueueException(
                    $"Failed to enqueue record(s). The LUA enqueue script returned the wrong count of items {result.Count}, expected {messageCount * 2}");
            }

            for (var i = 0; i < result.Count; i = i + 2) //note that we pull items as pairs
            {
                rc.Add(new QueueOutputMessage(sentMessageFactory.Create(new RedisQueueId(result[i]),
                     new RedisQueueCorrelationId(new Guid(result[i + 1])))));
            }
            return rc;
        }
    }
}
