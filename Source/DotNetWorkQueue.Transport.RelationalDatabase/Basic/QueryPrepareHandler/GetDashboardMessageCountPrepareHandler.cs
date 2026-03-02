// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    internal class GetDashboardMessageCountPrepareHandler : IPrepareQueryHandler<GetDashboardMessageCountQuery, long>
    {
        private readonly CommandStringCache _commandCache;

        public GetDashboardMessageCountPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(GetDashboardMessageCountQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            var sql = _commandCache.GetCommand(CommandStringTypes.GetDashboardMessageCount);

            if (query.StatusFilter.HasValue)
            {
                // Append WHERE clause to the count query
                sql += " WHERE Status = @Status";

                var status = dbCommand.CreateParameter();
                status.ParameterName = "@Status";
                status.DbType = DbType.Int32;
                status.Value = query.StatusFilter.Value;
                dbCommand.Parameters.Add(status);
            }

            dbCommand.CommandText = sql;
        }
    }
}
