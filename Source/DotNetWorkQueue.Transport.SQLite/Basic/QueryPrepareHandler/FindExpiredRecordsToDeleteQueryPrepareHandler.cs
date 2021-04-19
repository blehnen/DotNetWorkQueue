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
using System.Collections.Generic;
using System.Data;
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
    public class FindExpiredRecordsToDeleteQueryPrepareHandler : IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery<long>, IEnumerable<long>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryPrepareHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="timeFactory">The time factory.</param>
        public FindExpiredRecordsToDeleteQueryPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory timeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => timeFactory, timeFactory);
            _commandCache = commandCache;
            _getTime = timeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(FindExpiredMessagesToDeleteQuery<long> query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var command = dbCommand;
            var param = command.CreateParameter();
            param.ParameterName = "@CurrentDateTime";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().Ticks;
            command.Parameters.Add(param);
        }
    }
}
