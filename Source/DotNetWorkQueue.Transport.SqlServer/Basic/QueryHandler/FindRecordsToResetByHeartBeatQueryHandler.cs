// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler
        : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configuration">The configuration.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(SqlServerCommandStringCache commandCache,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configuration, configuration);

            _commandCache = commandCache;
            _configuration = configuration;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IEnumerable<MessageToReset> Handle(FindMessagesToResetByHeartBeatQuery query)
        {
            using (var connection = new SqlConnection(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        _commandCache.GetCommand(SqlServerCommandStringTypes.GetHeartBeatExpiredMessageIds);
                    command.Parameters.Add("@Time", SqlDbType.BigInt);
                    command.Parameters["@Time"].Value = _configuration.HeartBeat.Time.TotalSeconds;
                    command.Parameters.Add("@Status", SqlDbType.Int);
                    command.Parameters["@Status"].Value = Convert.ToInt16(QueueStatus.Processing);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            yield return new MessageToReset(reader.GetInt64(0), reader.GetDateTime(1));
                        }
                    }
                }
            }
        }
    }
}
