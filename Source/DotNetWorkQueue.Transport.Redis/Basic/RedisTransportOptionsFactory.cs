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
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Loads saved transport options from Redis, or returns defaults.
    /// </summary>
    public class RedisTransportOptionsFactory
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private RedisBaseTransportOptions _options;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisTransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="connection">The Redis connection.</param>
        /// <param name="redisNames">The Redis key names.</param>
        public RedisTransportOptionsFactory(IRedisConnection connection, RedisNames redisNames)
        {
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <summary>Test seam — returns an <see cref="IDatabase"/> to execute against.</summary>
        protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

        /// <summary>
        /// Creates or returns the cached transport options.
        /// </summary>
        /// <returns>The Redis transport options.</returns>
        public RedisBaseTransportOptions Create()
        {
            if (_options != null) return _options;
            lock (_lock)
            {
                if (_options != null) return _options;

                try
                {
                    var json = GetDb().StringGet(_redisNames.Configuration);
                    if (json.HasValue)
                    {
                        var loaded = JsonConvert.DeserializeObject<RedisBaseTransportOptions>(json);
                        if (loaded != null)
                        {
                            _options = loaded;
                            return _options;
                        }
                    }
                }
                catch
                {
                    // Load failed — fall through to uncached defaults so a subsequent
                    // Create() can re-attempt the read once Redis is available.
                }

                // Queue has no persisted options yet — return defaults but do NOT cache.
                // A subsequent Create() after options are persisted must observe the new value.
                return new RedisBaseTransportOptions();
            }
        }
    }
}
