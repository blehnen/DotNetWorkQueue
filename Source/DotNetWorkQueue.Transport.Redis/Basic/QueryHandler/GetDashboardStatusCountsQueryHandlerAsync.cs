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
    internal class GetDashboardStatusCountsQueryHandlerAsync : IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        public GetDashboardStatusCountsQueryHandlerAsync(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        public Task<DashboardStatusCounts> HandleAsync(GetDashboardStatusCountsQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var total = (long)db.HashLength(_redisNames.MetaData);
            var processing = (long)db.SortedSetLength(_redisNames.Working);
            var error = (long)db.ListLength(_redisNames.Error);
            var waiting = total - processing - error;
            return Task.FromResult(new DashboardStatusCounts
            {
                Waiting = waiting < 0 ? 0 : waiting,
                Processing = processing,
                Error = error,
                Total = total
            });
        }
    }
}
