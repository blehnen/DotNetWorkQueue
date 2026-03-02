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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    internal class GetDashboardErrorMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _internalSerializer;

        public GetDashboardErrorMessagesQueryHandlerAsync(
            IRedisConnection connection,
            RedisNames redisNames,
            IInternalSerializer internalSerializer)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => internalSerializer, internalSerializer);

            _connection = connection;
            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
        }

        public Task<IReadOnlyList<DashboardErrorMessage>> HandleAsync(GetDashboardErrorMessagesQuery query)
        {
            var db = _connection.Connection.GetDatabase();

            // Get all error IDs, apply paging
            var errorIds = db.ListRange(_redisNames.Error, 0, -1)
                .Select(x => x.ToString())
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var result = new List<DashboardErrorMessage>();
            foreach (var id in errorIds)
            {
                var metaBytes = (byte[])db.HashGet(_redisNames.MetaData, id);
                long queueDateTime = 0;
                if (metaBytes != null && metaBytes.Length > 0)
                {
                    var meta = _internalSerializer.ConvertBytesTo<RedisMetaData>(metaBytes);
                    queueDateTime = meta?.QueueDateTime ?? 0;
                }

                // Get error time from sorted set
                var errorScore = db.SortedSetScore(_redisNames.ErrorTime, id);
                DateTimeOffset? lastExceptionDate = errorScore.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)errorScore.Value)
                    : (DateTimeOffset?)null;

                result.Add(new DashboardErrorMessage
                {
                    QueueId = id,
                    Status = 2,
                    QueuedDateTime = queueDateTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(queueDateTime) : (DateTimeOffset?)null,
                    LastException = null, // Redis has no exception detail
                    LastExceptionDate = lastExceptionDate
                });
            }
            return Task.FromResult<IReadOnlyList<DashboardErrorMessage>>(result);
        }
    }
}
