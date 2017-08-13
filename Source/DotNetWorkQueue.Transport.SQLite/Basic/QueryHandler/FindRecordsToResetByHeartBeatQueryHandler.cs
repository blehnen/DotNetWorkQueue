// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler
        : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(SqLiteCommandStringCache commandCache,
            QueueConsumerConfiguration configuration,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _commandCache = commandCache;
            _configuration = configuration;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public IEnumerable<MessageToReset> Handle(FindMessagesToResetByHeartBeatQuery query)
        {
            var results = new List<MessageToReset>();
            if (!DatabaseExists.Exists(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString))
            {
                return results;
            }

            if (query.Cancellation.IsCancellationRequested)
            {
                return results;
            }

            using (var connection = new SQLiteConnection(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString))
            {
                connection.Open();

                //before executing a query, double check that we aren't stopping
                //otherwise, there is a chance that the tables no longer exist in memory mode
                if (query.Cancellation.IsCancellationRequested)
                {
                    return results;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        _commandCache.GetCommand(CommandStringTypes.GetHeartBeatExpiredMessageIds);
                    command.Parameters.Add("@time", DbType.Int64);
                    var selectTime = _getTime.GetCurrentUtcDate().AddSeconds(_configuration.HeartBeat.Time.TotalSeconds * -1);
                    command.Parameters["@time"].Value = selectTime.Ticks;
                    command.Parameters.Add("@Status", DbType.Int32);
                    command.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Processing);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            results.Add(new MessageToReset(reader.GetInt64(0), DateTime.FromBinary(reader.GetInt64(1))));
                        }
                    }
                }
            }
            return results;
        }
    }
}
