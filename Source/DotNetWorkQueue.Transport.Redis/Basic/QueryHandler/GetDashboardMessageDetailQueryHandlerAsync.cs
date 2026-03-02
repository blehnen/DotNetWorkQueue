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
    internal class GetDashboardMessageDetailQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly RedisHeaders _redisHeaders;
        private readonly IInternalSerializer _internalSerializer;
        private readonly ICompositeSerialization _serialization;

        public GetDashboardMessageDetailQueryHandlerAsync(
            IRedisConnection connection,
            RedisNames redisNames,
            RedisHeaders redisHeaders,
            IInternalSerializer internalSerializer,
            ICompositeSerialization serialization)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => redisHeaders, redisHeaders);
            Guard.NotNull(() => internalSerializer, internalSerializer);
            Guard.NotNull(() => serialization, serialization);

            _connection = connection;
            _redisNames = redisNames;
            _redisHeaders = redisHeaders;
            _internalSerializer = internalSerializer;
            _serialization = serialization;
        }

        public Task<DashboardMessage> HandleAsync(GetDashboardMessageDetailQuery query)
        {
            var db = _connection.Connection.GetDatabase();
            var metaBytes = (byte[])db.HashGet(_redisNames.MetaData, query.MessageId);
            if (metaBytes == null || metaBytes.Length == 0)
                return Task.FromResult<DashboardMessage>(null);

            var meta = _internalSerializer.ConvertBytesTo<RedisMetaData>(metaBytes);

            // Determine status; heartbeat is the score in the Working sorted set
            int status;
            var workingScore = db.SortedSetScore(_redisNames.Working, query.MessageId);
            if (workingScore.HasValue)
            {
                status = 1;
            }
            else
            {
                var errorIds = db.ListRange(_redisNames.Error, 0, -1).Select(x => x.ToString());
                status = errorIds.Contains(query.MessageId) ? 2 : 0;
            }

            var message = new DashboardMessage
            {
                QueueId = query.MessageId,
                Status = status,
                QueuedDateTime = meta?.QueueDateTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(meta.QueueDateTime) : (DateTimeOffset?)null
            };

            // HeartBeat is stored as the score (unix timestamp MS) in the Working sorted set
            if (workingScore.HasValue && workingScore.Value > 0)
                message.HeartBeat = DateTimeOffset.FromUnixTimeMilliseconds((long)workingScore.Value);

            // Expiration time is stored as a score (unix timestamp MS) in the Expiration sorted set
            var expirationScore = db.SortedSetScore(_redisNames.Expiration, query.MessageId);
            if (expirationScore.HasValue && expirationScore.Value > 0)
                message.ExpirationTime = DateTimeOffset.FromUnixTimeMilliseconds((long)expirationScore.Value);

            // Delayed processing time is stored as a score in the Delayed sorted set
            var delayedScore = db.SortedSetScore(_redisNames.Delayed, query.MessageId);
            if (delayedScore.HasValue && delayedScore.Value > 0)
                message.QueueProcessTime = DateTimeOffset.FromUnixTimeMilliseconds((long)delayedScore.Value);

            // Route is stored in the Route hash
            var route = db.HashGet(_redisNames.Route, query.MessageId);
            if (route.HasValue)
                message.Route = route.ToString();

            // Correlation ID is stored inside the message headers
            try
            {
                var headerBytes = (byte[])db.HashGet(_redisNames.Headers, query.MessageId);
                if (headerBytes != null && headerBytes.Length > 0)
                {
                    var headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerBytes);
                    if (headers.TryGetValue(_redisHeaders.CorrelationId.Name, out var corrValue) && corrValue != null)
                    {
                        if (corrValue is RedisQueueCorrelationIdSerialized serializedId)
                            message.CorrelationId = serializedId.Id.ToString();
                        else
                            message.CorrelationId = corrValue.ToString();
                    }
                }
            }
            catch
            {
                // Header deserialization failure should not prevent returning the message detail
            }

            return Task.FromResult(message);
        }
    }
}
