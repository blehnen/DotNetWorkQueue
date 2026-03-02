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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    internal class GetDashboardMessageDetailQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public GetDashboardMessageDetailQueryHandlerAsync(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public Task<DashboardMessage> HandleAsync(GetDashboardMessageDetailQuery query)
        {
            var id = int.Parse(query.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                // Check MetaData table first
                var meta = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                var metaRecord = meta.Query()
                    .Where(x => x.QueueId == id)
                    .FirstOrDefault();

                if (metaRecord != null)
                {
                    return Task.FromResult(new DashboardMessage
                    {
                        QueueId = metaRecord.QueueId.ToString(),
                        QueuedDateTime = new DateTimeOffset(metaRecord.QueuedDateTime, TimeSpan.Zero),
                        CorrelationId = metaRecord.CorrelationId.ToString(),
                        Status = (int)metaRecord.Status,
                        Priority = null,
                        QueueProcessTime = metaRecord.QueueProcessTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(metaRecord.QueueProcessTime.Value, TimeSpan.Zero)
                            : null,
                        HeartBeat = metaRecord.HeartBeat.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(metaRecord.HeartBeat.Value, TimeSpan.Zero)
                            : null,
                        ExpirationTime = metaRecord.ExpirationTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(metaRecord.ExpirationTime.Value, TimeSpan.Zero)
                            : null,
                        Route = metaRecord.Route
                    });
                }

                // Check MetaDataErrors table
                var errorCol = db.Database.GetCollection<Schema.MetaDataErrorsTable>(_tableNameHelper.MetaDataErrorsName);
                var errorRecord = errorCol.Query()
                    .Where(x => x.QueueId == id)
                    .FirstOrDefault();

                if (errorRecord != null)
                {
                    return Task.FromResult(new DashboardMessage
                    {
                        QueueId = errorRecord.QueueId.ToString(),
                        QueuedDateTime = new DateTimeOffset(errorRecord.QueuedDateTime, TimeSpan.Zero),
                        CorrelationId = errorRecord.CorrelationId.ToString(),
                        Status = (int)errorRecord.Status,
                        Priority = null,
                        QueueProcessTime = errorRecord.QueueProcessTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(errorRecord.QueueProcessTime.Value, TimeSpan.Zero)
                            : null,
                        HeartBeat = errorRecord.HeartBeat.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(errorRecord.HeartBeat.Value, TimeSpan.Zero)
                            : null,
                        ExpirationTime = errorRecord.ExpirationTime.HasValue
                            ? (DateTimeOffset?)new DateTimeOffset(errorRecord.ExpirationTime.Value, TimeSpan.Zero)
                            : null,
                        Route = errorRecord.Route
                    });
                }

                return Task.FromResult<DashboardMessage>(null);
            }
        }
    }
}
