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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    internal class GetDashboardStaleMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _internalSerializer;
        //Staleness is measured against heartbeats the transport wrote, so the cut-off has to come from the clock that wrote them rather than this machine's.
        private readonly IGetTime _getTime;

        public GetDashboardStaleMessagesQueryHandlerAsync(            IRedisConnection connection,
            RedisNames redisNames,
            IInternalSerializer internalSerializer,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(connection);
            Guard.NotNull(redisNames);
            Guard.NotNull(internalSerializer);

            _connection = connection;
            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
            Guard.NotNull(getTimeFactory);
            _getTime = getTimeFactory.Create();
        }

        public Task<IReadOnlyList<DashboardMessage>> HandleAsync(GetDashboardStaleMessagesQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var cutoffMs = new DateTimeOffset(_getTime.GetCurrentUtcDate())
                .AddSeconds(-query.ThresholdSeconds).ToUnixTimeMilliseconds();

            // Get stale message IDs (in Working sorted set with score < cutoff)
            var staleIds = db.SortedSetRangeByScore(_redisNames.Working, 0, cutoffMs,
                skip: query.PageIndex * query.PageSize, take: query.PageSize);

            var result = new List<DashboardMessage>();
            foreach (var id in staleIds)
            {
                var idStr = id.ToString();
                var metaBytes = (byte[])db.HashGet(_redisNames.MetaData, idStr);
                long queueDateTime = 0;
                if (metaBytes != null && metaBytes.Length > 0)
                {
                    var meta = _internalSerializer.ConvertBytesTo<RedisMetaData>(metaBytes);
                    queueDateTime = meta?.QueueDateTime ?? 0;
                }
                result.Add(new DashboardMessage
                {
                    QueueId = idStr,
                    Status = 1, // Processing (stale)
                    QueuedDateTime = queueDateTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(queueDateTime) : (DateTimeOffset?)null
                });
            }
            return Task.FromResult<IReadOnlyList<DashboardMessage>>(result);
        }
    }
}
