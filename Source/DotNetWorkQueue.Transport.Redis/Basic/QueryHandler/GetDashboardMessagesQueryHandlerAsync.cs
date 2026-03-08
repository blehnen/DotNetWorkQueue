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
    internal class GetDashboardMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _internalSerializer;

        public GetDashboardMessagesQueryHandlerAsync(
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

        public Task<IReadOnlyList<DashboardMessage>> HandleAsync(GetDashboardMessagesQuery query)
        {
            var db = _connection.Connection.GetDatabase();

            // Get ALL entries from MetaData hash
            var allEntries = db.HashGetAll(_redisNames.MetaData);

            // Get working (processing) IDs and error IDs for status determination
            var workingIds = new HashSet<string>(
                db.SortedSetRangeByRank(_redisNames.Working, 0, -1).Select(x => x.ToString()));
            var errorIds = new HashSet<string>(
                db.ListRange(_redisNames.Error, 0, -1).Select(x => x.ToString()));

            // Get delayed processing times from the Delayed sorted set
            var delayedEntries = db.SortedSetRangeByRankWithScores(_redisNames.Delayed, 0, -1);
            var delayedTimes = new Dictionary<string, double>();
            foreach (var d in delayedEntries)
                delayedTimes[d.Element.ToString()] = d.Score;

            var result = new List<DashboardMessage>();
            foreach (var entry in allEntries)
            {
                var id = entry.Name.ToString();
                int status;
                if (workingIds.Contains(id)) status = 1; // Processing
                else if (errorIds.Contains(id)) status = 2; // Error
                else status = 0; // Waiting

                // Apply filter
                if (query.StatusFilter.HasValue && query.StatusFilter.Value != status) continue;

                long queueDateTime = 0;
                if (entry.Value.HasValue)
                {
                    var bytes = (byte[])entry.Value;
                    if (bytes != null && bytes.Length > 0)
                    {
                        var meta = _internalSerializer.ConvertBytesTo<RedisMetaData>(bytes);
                        queueDateTime = meta?.QueueDateTime ?? 0;
                    }
                }

                var msg = new DashboardMessage
                {
                    QueueId = id,
                    Status = status,
                    QueuedDateTime = queueDateTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(queueDateTime) : (DateTimeOffset?)null
                };

                if (delayedTimes.TryGetValue(id, out var delayedScore) && delayedScore > 0)
                    msg.QueueProcessTime = DateTimeOffset.FromUnixTimeMilliseconds((long)delayedScore);

                result.Add(msg);
            }

            // Apply paging
            var paged = result.Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();
            return Task.FromResult<IReadOnlyList<DashboardMessage>>(paged);
        }
    }
}
