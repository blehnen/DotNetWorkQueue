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
using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="MessageToReset{T}" />
    public class FindRecordsToResetByHeartBeatQueryPrepareHandler : IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<long>, IEnumerable<MessageToReset<long>>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public FindRecordsToResetByHeartBeatQueryPrepareHandler(CommandStringCache commandCache,
            QueueConsumerConfiguration configuration, IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);

            _configuration = configuration;
            _getTime = getTimeFactory.Create();
            _commandCache = commandCache;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(FindMessagesToResetByHeartBeatQuery<long> query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText =
                _commandCache.GetCommand(commandType);

            var command = dbCommand;

            var param = command.CreateParameter();
            param.ParameterName = "@Time";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().AddSeconds(_configuration.HeartBeat.Time.TotalSeconds * -1).Ticks;
            command.Parameters.Add(param);

            param = command.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            command.Parameters.Add(param);
        }
    }
}
