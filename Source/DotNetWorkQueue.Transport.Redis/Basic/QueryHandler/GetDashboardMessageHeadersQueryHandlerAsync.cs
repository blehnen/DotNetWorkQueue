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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    internal class GetDashboardMessageHeadersQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        public GetDashboardMessageHeadersQueryHandlerAsync(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        public Task<DashboardMessageHeaders> HandleAsync(GetDashboardMessageHeadersQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var headerBytes = (byte[])db.HashGet(_redisNames.Headers, query.MessageId);
            if (headerBytes == null)
                return Task.FromResult<DashboardMessageHeaders>(null);
            return Task.FromResult(new DashboardMessageHeaders { Headers = headerBytes });
        }
    }
}
