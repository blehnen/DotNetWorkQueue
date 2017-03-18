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
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Sends a heartbeat for a single message
    /// </summary>
    /// <remarks>client does not support options of 'zadd'; we are using LUA script to work around this</remarks>
    internal class SendHeartbeatLua: BaseLua
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartbeatLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public SendHeartbeatLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"redis.call('zadd', @workingkey, 'XX', @timestamp, @uuid) ";
        }
        /// <summary>
        /// Sends a heartbeat for a single message
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        public int? Execute(string messageid, long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            var db = Connection.Connection.GetDatabase();
            return (int)db.ScriptEvaluate(LoadedLuaScript, GetParameters(messageid, unixTime));
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="messageid">The messageid.</param>
        /// <param name="unixTime">The unix time.</param>
        /// <returns></returns>
        private object GetParameters(string messageid, long unixTime)
        {
            return new
            {
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                uuid = messageid,
            };
        }

    }
}
