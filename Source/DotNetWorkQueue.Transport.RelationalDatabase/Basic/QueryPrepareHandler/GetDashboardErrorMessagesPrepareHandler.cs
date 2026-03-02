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
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    internal class GetDashboardErrorMessagesPrepareHandler : IPrepareQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>
    {
        private readonly CommandStringCache _commandCache;

        public GetDashboardErrorMessagesPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(GetDashboardErrorMessagesQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.GetDashboardErrorMessages);

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
