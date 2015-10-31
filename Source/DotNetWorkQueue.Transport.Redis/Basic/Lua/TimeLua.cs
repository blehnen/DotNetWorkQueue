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
using StackExchange.Redis;
namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// Gets the current time from a redis server
    /// </summary>
    internal class TimeLua: BaseLua
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeLua"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        public TimeLua(IRedisConnection connection, RedisNames redisNames)
            : base(connection, redisNames)
        {
            Script = @"return redis.call('time')";
        }
        /// <summary>
        /// Gets the current time from a redis server
        /// </summary>
        /// <returns></returns>
        public long Execute()
        {
            var db = Connection.Connection.GetDatabase();
            var result = (RedisValue[])db.ScriptEvaluate(LoadedLuaScript);
            var seconds = (long) result[0];
            var milliseconds = ((long) result[1]/1000); //convert microseconds to milliseconds
            return (long)TimeSpan.FromSeconds(seconds).Add(TimeSpan.FromMilliseconds(milliseconds)).TotalMilliseconds;
        }
    }
}
