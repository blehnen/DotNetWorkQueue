// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <inheritdoc />
    /// <summary>
    /// Dequeues the next record
    /// </summary>
    internal class DequeueLua : BaseLua
    {
        private string[] _routes;
        private int _nextRoute;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly object _routeInit = new object();
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DequeueLua" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="configuration">The configuration.</param>
        public DequeueLua(IRedisConnection connection, RedisNames redisNames, QueueConsumerConfiguration configuration)
            : base(connection, redisNames)
        {
            _configuration = configuration;
            Script = @"local uuid = redis.call('rpop', @pendingkey) 
                    if (uuid==false) then 
                        return nil;
                    end        
                    local expireScore = redis.call('zscore', @expirekey, uuid)
                    local message = redis.call('hget', @valueskey, uuid) 
                    local headers = redis.call('hget', @headerskey, uuid)
                    if(message) then
                        redis.call('zadd', @workingkey, @timestamp, uuid)
                        redis.call('hset', @StatusKey, uuid, '1') 
                        return {uuid, message, headers, expireScore} 
                    else
                        return {uuid, '', '', ''}
                    end";
                    
        }
        /// <summary>
        /// Dequeues the next record
        /// </summary>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public RedisValue[] Execute(long unixTime )
        {
            if (Connection.IsDisposed)
                return null;

            InitRoutes();

            var db = Connection.Connection.GetDatabase();

            if (_routes == null)
            {
                var result = TryExecute(GetParameters(unixTime, null));
                if (!result.IsNull)
                {
                    return (RedisValue[]) result;
                }
                return null;
            }
            var counter = 0;
            while (counter < _routes.Length)
            {
                var route = _routes[GetNextRoute()];

                var result = TryExecute(GetParameters(unixTime, route));
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
                counter++;
            }
            return null;
        }

        /// <summary>
        /// Dequeues the next record
        /// </summary>
        /// <param name="unixTime">The current unix time.</param>
        /// <returns></returns>
        public async Task<RedisValue[]> ExecuteAsync(long unixTime)
        {
            if (Connection.IsDisposed)
                return null;

            InitRoutes();

            var db = Connection.Connection.GetDatabase();
            if (_routes == null)
            {
                var result = await TryExecuteAsync(GetParameters(unixTime, null)).ConfigureAwait(false);
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
                return null;
            }

            var counter = 0;
            while (counter < _routes.Length)
            {
                var route = _routes[GetNextRoute()];
                var result = await TryExecuteAsync(GetParameters(unixTime, route)).ConfigureAwait(false);
                if (!result.IsNull)
                {
                    return (RedisValue[])result;
                }
                counter++;
            }

            return null;
        }

        private int GetNextRoute()
        {
            if (_routes.Length == 1)
                return 0;

            var number = Interlocked.Increment(ref _nextRoute);
            if (number < _routes.Length) return number;
            Interlocked.Exchange(ref _nextRoute, -1);
            return 0;
        }
        private void InitRoutes()
        {
            if (_routes != null || _configuration.Routes == null || _configuration.Routes.Count == 0) return;
            lock (_routeInit)
            {
                if (_routes != null) return;
                _routes = _configuration.Routes.ToArray();
                _nextRoute = -1;
            }
        }
        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="unixTime">The current unix time.</param>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        private object GetParameters(long unixTime, string route)
        {
            var pendingKey = !string.IsNullOrEmpty(route) ? RedisNames.PendingRoute(route) : RedisNames.Pending;
            return new
            {
                pendingkey = (RedisKey)pendingKey,
                workingkey = (RedisKey)RedisNames.Working,
                timestamp = unixTime,
                valueskey = (RedisKey)RedisNames.Values,
                headerskey = (RedisKey)RedisNames.Headers,
                expirekey = (RedisKey)RedisNames.Expiration,
                StatusKey = (RedisKey)RedisNames.Status
            };
        }
    }
}
