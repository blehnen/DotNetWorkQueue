// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.ObjectModel;
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler : IQueryHandler<FindMessagesToResetByHeartBeatQuery<int>, IEnumerable<MessageToReset<int>>>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ICompositeSerialization _serialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="serialization">The serialization.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper,
            IHeartBeatConfiguration configuration,
            ICompositeSerialization serialization)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => serialization, serialization);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _configuration = configuration;
            _serialization = serialization;
        }

        /// <inheritdoc />
        public IEnumerable<MessageToReset<int>> Handle(FindMessagesToResetByHeartBeatQuery<int> query)
        {
            if (query.Cancellation.IsCancellationRequested)
            {
                return Enumerable.Empty<MessageToReset<int>>();
            }

            using (var db = new LiteDatabase(_connectionInformation.ConnectionString))
            {
                //before executing a query, double check that we aren't stopping
                //otherwise, there is a chance that the tables no longer exist in memory mode
                if (query.Cancellation.IsCancellationRequested)
                {
                    return Enumerable.Empty<MessageToReset<int>>();
                }

                var col = db.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);
                var date = DateTime.UtcNow.Subtract(_configuration.Time);

                var results = col.Query()
                    .Where(x => x.Status == QueueStatuses.Processing)
                    .Where(x => x.HeartBeat.HasValue && x.HeartBeat.Value < date)
                    .ToList();

                var data = new List<MessageToReset<int>>(results.Count);
                var queue = db.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                foreach (var record in results)
                {
                    if (record.HeartBeat.HasValue)
                    {
                        var queueRecord = queue.FindById(record.QueueId);
                        if (queueRecord != null)
                        {
                            var headers = _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(queueRecord.Headers);
                            var reset = new MessageToReset<int>(record.QueueId, record.HeartBeat.Value, new ReadOnlyDictionary<string, object>(headers));
                            data.Add(reset);
                        }
                    }
                }

                return data;
            }
        }
    }
}
