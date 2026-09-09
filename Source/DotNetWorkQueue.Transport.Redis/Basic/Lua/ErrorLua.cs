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
using StackExchange.Redis;

using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Moves a message from the working queue to the error queue
    /// </summary>
    internal class ErrorLua : BaseLua
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public ErrorLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zrem', @workingkey, @uuid)
                     redis.call('lpush', @errorkey, @uuid)
                     redis.call('zadd', @errortimekey, @timestamp, @uuid)
                     redis.call('hset', @StatusKey, @uuid, '2')
                     return 1";
        }
        /// <summary>
        /// Executes the specified message identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">current time</param>
        /// <returns></returns>
        public int? Execute(string messageId, long unixTime)
        {
            var result = TryExecute(GetParameters(messageId, unixTime));
            if (result.IsNull)
                return null;
            return (int)result;
        }
        /// <summary>
        /// Moves a message to the error queue without blocking a thread across the call.
        /// </summary>
        /// <remarks>
        /// A line-for-line twin of <see cref="Execute"/>, differing only in awaiting the script. It
        /// deliberately omits the <c>Connection.IsDisposed</c> guard that <c>DequeueLua.ExecuteAsync</c>
        /// opens with, because the synchronous method here does not have one either - matching its own
        /// twin matters more than matching the other script.
        /// </remarks>
        public async Task<int?> ExecuteAsync(string messageId, long unixTime)
        {
            var result = await TryExecuteAsync(GetParameters(messageId, unixTime)).ConfigureAwait(false);
            if (result.IsNull)
                return null;
            return (int)result;
        }
        /// <summary>Gets the parameters.</summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="unixTime">current time</param>
        /// <returns></returns>
        private object GetParameters(string messageId, long unixTime)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                errortimekey = (RedisKey)RedisNames.ErrorTime,
                errorkey = (RedisKey)RedisNames.Error,
                StatusKey = (RedisKey)RedisNames.Status,
                timestamp = unixTime
            };
        }
    }
}
