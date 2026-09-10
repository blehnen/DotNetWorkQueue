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
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    /// <summary>
    /// The asynchronous twin of <see cref="ReceiveMessageQueryHandler"/>. Both derive from
    /// <see cref="AReceiveMessageQueryHandler"/>, so the only difference between them is the call
    /// that runs the Lua script.
    /// </summary>
    internal class ReceiveMessageQueryHandlerAsync : AReceiveMessageQueryHandler, IQueryHandlerAsync<ReceiveMessageQuery, RedisMessage>
    {
        /// <summary>Initializes a new instance of the <see cref="ReceiveMessageQueryHandlerAsync"/> class.</summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="removeMessage">Removes a message from the queue</param>
        /// <param name="redisHeaders">The redisHeaders.</param>
        /// <param name="dequeueLua">The dequeue.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        public ReceiveMessageQueryHandlerAsync(
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
        public async Task<RedisMessage> HandleAsync(ReceiveMessageQuery query)
        {
            long unixTimestamp;
            RedisValue[] result;
            try
            {
                unixTimestamp = UnixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
                result = await DequeueLua.ExecuteAsync(unixTimestamp).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                throw new ReceiveMessageException("Failed to dequeue a message", error);
            }

            var message = BuildMessage(query, unixTimestamp, result, out var expired);
            if (!expired) return message;

            //The one thing this cannot share with the synchronous handler: an expired message is
            //removed here, on the async receive path, so it must not block the continuation.
            try
            {
                await RemoveMessage.RemoveAsync(query.MessageContext, RemoveMessageReason.Expired)
                    .ConfigureAwait(false);
            }
            catch (Exception error)
            {
                throw new ReceiveMessageException("Failed to dequeue a message", error);
            }
            return message;
        }
    }
}
