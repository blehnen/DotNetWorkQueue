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

namespace DotNetWorkQueue.Transport.Memory.Basic.QueryHandler
{
    internal class GetDashboardMessagesQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly IDataStorage _dataStorage;

        public GetDashboardMessagesQueryHandlerAsync(IDataStorage dataStorage)
        {
            Guard.NotNull(() => dataStorage, dataStorage);
            _dataStorage = dataStorage;
        }

        public Task<IReadOnlyList<DashboardMessage>> HandleAsync(GetDashboardMessagesQuery query)
        {
            var results = new List<DashboardMessage>();
            var skip = query.PageIndex * query.PageSize;

            if (query.StatusFilter == 2)
            {
                // Error — memory transport doesn't store error records
                return Task.FromResult<IReadOnlyList<DashboardMessage>>(results);
            }

            if (query.StatusFilter == 0)
            {
                // Waiting only
                var items = _dataStorage.GetWaitingMessages(skip, query.PageSize);
                foreach (var item in items)
                    results.Add(MapToMessage(item, 0));
            }
            else if (query.StatusFilter == 1)
            {
                // Processing only
                var items = _dataStorage.GetProcessingMessages(skip, query.PageSize);
                foreach (var item in items)
                    results.Add(MapToMessage(item, 1));
            }
            else
            {
                // All (null filter) — concat waiting + processing, then page
                var waiting = _dataStorage.GetWaitingMessages(0, int.MaxValue);
                var processing = _dataStorage.GetProcessingMessages(0, int.MaxValue);

                var all = waiting.Select(w => MapToMessage(w, 0))
                    .Concat(processing.Select(p => MapToMessage(p, 1)))
                    .Skip(skip)
                    .Take(query.PageSize)
                    .ToList();

                results.AddRange(all);
            }

            return Task.FromResult<IReadOnlyList<DashboardMessage>>(results);
        }

        private static DashboardMessage MapToMessage(QueueItem item, int status)
        {
            return new DashboardMessage
            {
                QueueId = item.Id.ToString(),
                QueuedDateTime = item.QueuedDateTime == DateTime.MinValue
                    ? (DateTimeOffset?)null
                    : new DateTimeOffset(item.QueuedDateTime, TimeSpan.Zero),
                CorrelationId = item.CorrelationId.ToString(),
                Status = status
            };
        }
    }
}
