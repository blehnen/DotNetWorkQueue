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
using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Number of pending delayed records
    /// </summary>
    internal class GetPendingDelayedCountHandler : IQueryHandler<GetPendingDelayedCountQuery, long>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public GetPendingDelayedCountHandler(SqLiteCommandStringCache commandCache,
            IConnectionInformation connectionInformation,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public long Handle(GetPendingDelayedCountQuery query)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return 0;
            }

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.GetPendingDelayCount);
                    command.Parameters.Add("@CurrentDateTime", DbType.Int64);
                    command.Parameters["@CurrentDateTime"].Value = _getTime.GetCurrentUtcDate().Ticks;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }
                    }
                }
            }
            return 0;
        }
    }
}
