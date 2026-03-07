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
using System.Data;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    internal class GetDashboardMessageDetailPrepareHandler : IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>
    {
        private readonly CommandStringCache _commandCache;
        private readonly Lazy<string> _dynamicColumns;

        public GetDashboardMessageDetailPrepareHandler(CommandStringCache commandCache, ITransportOptionsFactory optionsFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            _commandCache = commandCache;
            _dynamicColumns = new Lazy<string>(() => DashboardDynamicColumnHelper.BuildDynamicColumns(optionsFactory.Create()));
        }

        public void Handle(GetDashboardMessageDetailQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = string.Format(_commandCache.GetCommand(commandType), _dynamicColumns.Value);

            var queueId = dbCommand.CreateParameter();
            queueId.ParameterName = "@QueueId";
            queueId.DbType = DbType.Int64;
            queueId.Value = long.Parse(query.MessageId);
            dbCommand.Parameters.Add(queueId);
        }
    }
}
