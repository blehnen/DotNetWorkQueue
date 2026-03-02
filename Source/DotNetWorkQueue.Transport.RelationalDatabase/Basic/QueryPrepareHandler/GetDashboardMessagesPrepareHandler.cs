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
using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    internal class GetDashboardMessagesPrepareHandler : IPrepareQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly Lazy<string> _dynamicColumns;

        public GetDashboardMessagesPrepareHandler(CommandStringCache commandCache, ITransportOptionsFactory optionsFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            _commandCache = commandCache;
            _dynamicColumns = new Lazy<string>(() => DashboardDynamicColumnHelper.BuildDynamicColumns(optionsFactory.Create()));
        }

        public void Handle(GetDashboardMessagesQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            var sql = string.Format(_commandCache.GetCommand(CommandStringTypes.GetDashboardMessages), _dynamicColumns.Value);

            if (query.StatusFilter.HasValue)
            {
                // Insert WHERE clause before ORDER BY
                var orderByIndex = sql.IndexOf("ORDER BY", System.StringComparison.OrdinalIgnoreCase);
                if (orderByIndex > 0)
                    sql = sql.Insert(orderByIndex, "WHERE Status = @Status ");

                var status = dbCommand.CreateParameter();
                status.ParameterName = "@Status";
                status.DbType = DbType.Int32;
                status.Value = query.StatusFilter.Value;
                dbCommand.Parameters.Add(status);
            }

            dbCommand.CommandText = sql;

            var offset = dbCommand.CreateParameter();
            offset.ParameterName = "@Offset";
            offset.DbType = DbType.Int32;
            offset.Value = query.PageIndex * query.PageSize;
            dbCommand.Parameters.Add(offset);

            var pageSize = dbCommand.CreateParameter();
            pageSize.ParameterName = "@PageSize";
            pageSize.DbType = DbType.Int32;
            pageSize.Value = query.PageSize;
            dbCommand.Parameters.Add(pageSize);
        }
    }
}
