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

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    internal class GetDashboardStaleMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardStaleMessagesQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<IReadOnlyList<DashboardMessage>> HandleAsync(GetDashboardStaleMessagesQuery query)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-query.ThresholdSeconds);

            using (var db = _connectionInformation.GetDatabase())
            {
                var meta = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                var records = meta.Query()
                    .Where(x => x.Status == QueueStatuses.Processing && x.HeartBeat != null && x.HeartBeat < cutoff)
                    .Skip(query.PageIndex * query.PageSize)
                    .Limit(query.PageSize)
                    .ToList();

                var results = new List<DashboardMessage>(records.Count);
                foreach (var record in records)
                {
                    results.Add(new DashboardMessage
                    {
                        QueueId = record.QueueId.ToString(),
                        QueuedDateTime = new DateTimeOffset(record.QueuedDateTime, TimeSpan.Zero),
                        CorrelationId = record.CorrelationId.ToString(),
                        Status = (int)record.Status,
                        Priority = null,
                        QueueProcessTime = record.QueueProcessTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(record.QueueProcessTime.Value, TimeSpan.Zero)
                            : null,
                        HeartBeat = record.HeartBeat.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(record.HeartBeat.Value, TimeSpan.Zero)
                            : null,
                        ExpirationTime = record.ExpirationTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(record.ExpirationTime.Value, TimeSpan.Zero)
                            : null,
                        Route = record.Route
                    });
                }

                return Task.FromResult<IReadOnlyList<DashboardMessage>>(results);
            }
        }
    }
}
