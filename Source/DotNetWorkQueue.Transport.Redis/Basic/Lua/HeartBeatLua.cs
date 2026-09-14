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
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Refreshes a message's place in the working set, but only while the claim is still the caller's.
    /// </summary>
    /// <remarks>
    /// The score of a member in the working set is its heartbeat, so refreshing a claim means writing a
    /// new score. Doing that with ZADD alone asks only whether the message is in the set, which cannot
    /// tell "my claim" from "the claim another worker now holds on the same message". After the monitor
    /// has reset a message and a second worker has taken it, an unconditional beat from the first worker
    /// refreshes the new owner's claim and both carry on processing.
    ///
    /// Comparing the score before writing closes that, and it has to happen inside the script: reading
    /// the score and then writing it from the client leaves the same race in a smaller window. A caller
    /// that has not written a score yet passes nothing and the comparison is skipped - that is the first
    /// beat, and a message cannot be taken from a worker until its claim is older than the expiry, by
    /// which point the caller has a score of its own to check against (GitHub #328).
    /// </remarks>
    internal class HeartBeatLua : BaseLua
    {
        /// <inheritdoc />
        public HeartBeatLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"local current = redis.call('zscore', @workingkey, @uuid)
                     if(current == false) then
                        return 0
                     end
                     if(@previous ~= '' and current ~= @previous) then
                        return 0
                     end
                     redis.call('zadd', @workingkey, @timestamp, @uuid)
                     return @timestamp";
        }

        /// <summary>
        /// Refreshes the claim, returning the new heartbeat or 0 if the message is no longer the
        /// caller's to refresh.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timestamp">The heartbeat to write.</param>
        /// <param name="previousTimestamp">The heartbeat this caller last wrote, or null on its first beat.</param>
        public virtual long Execute(string messageId, long timestamp, long? previousTimestamp)
        {
            var result = TryExecute(GetParameters(messageId, timestamp, previousTimestamp));
            if (result.IsNull)
                return 0;
            return (long)result;
        }

        /// <summary>
        /// Refreshes the claim without blocking a thread across the call.
        /// </summary>
        public virtual async Task<long> ExecuteAsync(string messageId, long timestamp, long? previousTimestamp)
        {
            var result = await TryExecuteAsync(GetParameters(messageId, timestamp, previousTimestamp)).ConfigureAwait(false);
            if (result.IsNull)
                return 0;
            return (long)result;
        }

        private object GetParameters(string messageId, long timestamp, long? previousTimestamp)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                uuid = messageId,
                timestamp,
                //empty rather than nil: the script compares it as a string, and a nil argument would
                //make the comparison itself an error rather than a skipped check
                previous = previousTimestamp.HasValue
                    ? previousTimestamp.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty
            };
        }
    }
}
