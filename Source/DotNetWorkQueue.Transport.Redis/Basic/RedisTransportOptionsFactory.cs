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

        public RedisTransportOptionsFactory(IRedisConnection connection, RedisNames redisNames)
        {
            _connection = connection;
            _redisNames = redisNames;
        }

        public RedisBaseTransportOptions Create()
        {
            if (_options != null) return _options;
            lock (_lock)
            {
                if (_options == null)
                {
                    try
                    {
                        var db = _connection.Connection.GetDatabase();
                        var json = db.StringGet(_redisNames.Configuration);
                        if (json.HasValue)
                        {
                            _options = JsonConvert.DeserializeObject<RedisBaseTransportOptions>(json);
                        }
                    }
                    catch
                    {
                        // If load fails, use defaults
                    }

                    _options = _options ?? new RedisBaseTransportOptions();
                }
            }
            return _options;
        }
    }
}
