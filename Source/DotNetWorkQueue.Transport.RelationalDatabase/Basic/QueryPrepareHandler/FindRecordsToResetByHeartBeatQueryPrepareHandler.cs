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
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    /// <inheritdoc />
    public class FindRecordsToResetByHeartBeatQueryPrepareHandler<T> : IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<T>, IEnumerable<MessageToReset<T>>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryPrepareHandler{T}"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configuration">The configuration.</param>
        public FindRecordsToResetByHeartBeatQueryPrepareHandler(CommandStringCache commandCache,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configuration, configuration);
            _commandCache = commandCache;
            _configuration = configuration;
        }
        /// <inheritdoc />
        public void Handle(FindMessagesToResetByHeartBeatQuery<T> query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText =
                _commandCache.GetCommand(commandType);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@Time";
            param.DbType = DbType.Int64;
            param.Value = _configuration.HeartBeat.Time.TotalSeconds;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            dbCommand.Parameters.Add(param);

        }
    }
}
