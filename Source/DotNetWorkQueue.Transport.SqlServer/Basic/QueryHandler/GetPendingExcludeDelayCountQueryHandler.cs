// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Number of pending records, with the delayed records exlcuded
    /// </summary>
    internal class GetPendingExcludeDelayCountQueryHandler : IQueryHandler<GetPendingExcludeDelayCountQuery, long>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public GetPendingExcludeDelayCountQueryHandler(SqlServerCommandStringCache commandCache,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public long Handle(GetPendingExcludeDelayCountQuery query)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqlServerCommandStringTypes.GetPendingExcludeDelayCount);
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
