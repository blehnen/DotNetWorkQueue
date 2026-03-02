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
    internal class GetDashboardErrorMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardErrorMessagesQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<IReadOnlyList<DashboardErrorMessage>> HandleAsync(GetDashboardErrorMessagesQuery query)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                var errorCol = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                var records = errorCol.Query()
                    .Skip(query.PageIndex * query.PageSize)
                    .Limit(query.PageSize)
                    .ToList();

                var results = new List<DashboardErrorMessage>(records.Count);
                foreach (var record in records)
                {
                    results.Add(new DashboardErrorMessage
                    {
                        Id = record.Id,
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
                        Route = record.Route,
                        LastException = record.LastException,
                        LastExceptionDate = new DateTimeOffset(record.LastExceptionDate, TimeSpan.Zero)
                    });
                }

                return Task.FromResult<IReadOnlyList<DashboardErrorMessage>>(results);
            }
        }
    }
}
