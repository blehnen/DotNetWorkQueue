// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler
{
    /// <summary>
    /// Prepares the find error message command for execution
    /// </summary>
    public class FindErrorRecordsToDeleteQueryPrepareHandler : IPrepareQueryHandler<FindErrorMessagesToDeleteQuery<long>, IEnumerable<long>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IMessageErrorConfiguration _configuration;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindErrorRecordsToDeleteQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="timeFactory">The time factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FindErrorRecordsToDeleteQueryPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory timeFactory,
            IMessageErrorConfiguration configuration)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => timeFactory, timeFactory);
            _commandCache = commandCache;
            _configuration = configuration;
            _getTime = timeFactory.Create();
        }
        /// <inheritdoc />
        public void Handle(FindErrorMessagesToDeleteQuery<long> query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var command = (NpgsqlCommand)dbCommand;
            command.Parameters.Add("@CurrentDate", NpgsqlDbType.Timestamp);
            command.Parameters["@CurrentDate"].Value = _getTime.GetCurrentUtcDate().Subtract(_configuration.MessageAge);
        }
    }
}
