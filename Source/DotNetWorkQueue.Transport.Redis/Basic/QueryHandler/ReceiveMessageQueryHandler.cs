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
using StackExchange.Redis;
using System;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryHandler : AReceiveMessageQueryHandler, IQueryHandler<ReceiveMessageQuery, RedisMessage>
    {
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
            : base(serializer, receivedMessageFactory, removeMessage, redisHeaders, dequeueLua,
                unixTimeFactory, messageFactory)
        {
        }

        /// <inheritdoc />
        public RedisMessage Handle(ReceiveMessageQuery query)
        {
            long unixTimestamp;
            RedisValue[] result;
            try
            {
                unixTimestamp = UnixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
                result = DequeueLua.Execute(unixTimestamp);
            }
            catch (Exception error)
            {
                throw new ReceiveMessageException("Failed to dequeue a message", error);
            }

            var message = BuildMessage(query, unixTimestamp, result, out var expired);
            if (!expired) return message;

            //Kept inside the same exception contract the removal had when it lived in BuildMessage.
            try
            {
                RemoveMessage.Remove(query.MessageContext, RemoveMessageReason.Expired);
            }
            catch (Exception error)
            {
                throw new ReceiveMessageException("Failed to dequeue a message", error);
            }
            return message;
        }
    }
}
