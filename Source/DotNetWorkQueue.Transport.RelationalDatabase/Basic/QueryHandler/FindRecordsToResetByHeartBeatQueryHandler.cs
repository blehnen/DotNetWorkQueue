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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Finds records that are outside of the heartbeat window.
    /// </summary>
    internal class FindRecordsToResetByHeartBeatQueryHandler
        : IQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ISetupCommand _setupCommand;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="setupCommand">The setup command.</param>
        /// <param name="readColumn">The read column.</param>
        public FindRecordsToResetByHeartBeatQueryHandler(CommandStringCache commandCache,
            IDbConnectionFactory dbConnectionFactory,
            ISetupCommand setupCommand,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => setupCommand, setupCommand);
            Guard.NotNull(() => readColumn, readColumn);

            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
            _setupCommand = setupCommand;
            _readColumn = readColumn;
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

            if (query.Cancellation.IsCancellationRequested)
            {
                return results;
            }

            using (var connection = _dbConnectionFactory.Create())
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
                    _setupCommand.Setup(command, CommandStringTypes.GetHeartBeatExpiredMessageIds, query);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (query.Cancellation.IsCancellationRequested)
                            {
                                break;
                            }
                            results.Add(new MessageToReset(Convert.ToInt64(reader[0]), _readColumn.ReadAsDateTime(CommandStringTypes.GetHeartBeatExpiredMessageIds, 1, reader)));
                        }
                    }
                }
            }
            return results;
        }
    }
}
