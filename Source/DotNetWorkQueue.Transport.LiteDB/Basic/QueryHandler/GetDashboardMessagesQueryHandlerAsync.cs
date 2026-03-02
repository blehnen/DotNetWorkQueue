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
    internal class GetDashboardMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardMessagesQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<IReadOnlyList<DashboardMessage>> HandleAsync(GetDashboardMessagesQuery query)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                var results = new List<DashboardMessage>();

                if (query.StatusFilter == 2)
                {
                    // Error messages are in MetaDataErrors table
                    var errorCol = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                    var errorRecords = errorCol.Query()
                        .Skip(query.PageIndex * query.PageSize)
                        .Limit(query.PageSize)
                        .ToList();

                    foreach (var record in errorRecords)
                    {
                        results.Add(MapErrorToMessage(record));
                    }
                }
                else
                {
                    var meta = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                    List<Schema.MetaDataTable> records;

                    if (query.StatusFilter.HasValue)
                    {
                        var statusValue = (QueueStatuses)query.StatusFilter.Value;
                        records = meta.Query()
                            .Where(x => x.Status == statusValue)
                            .Skip(query.PageIndex * query.PageSize)
                            .Limit(query.PageSize)
                            .ToList();
                    }
                    else
                    {
                        records = meta.Query()
                            .Skip(query.PageIndex * query.PageSize)
                            .Limit(query.PageSize)
                            .ToList();
                    }

                    foreach (var record in records)
                    {
                        results.Add(MapMetaToMessage(record));
                    }
                }

                return Task.FromResult<IReadOnlyList<DashboardMessage>>(results);
            }
        }

        private static DashboardMessage MapMetaToMessage(Schema.MetaDataTable record)
        {
            return new DashboardMessage
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
            };
        }

        private static DashboardMessage MapErrorToMessage(Schema.MetaDataErrorsTable record)
        {
            return new DashboardMessage
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
            };
        }
    }
}
