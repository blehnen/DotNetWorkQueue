// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.QueryHandler
{
    internal class BuildDequeueCommand
    {
        private readonly IGetTime _getTime;
        public BuildDequeueCommand(IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _getTime = getTimeFactory.Create();
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "Query checked")]
        internal void BuildCommand(IDbCommand selectCommand, CommandString commandString,
            SqLiteMessageQueueTransportOptions options, List<string> routes)
        {
            selectCommand.CommandText = commandString.CommandText;

            var paramDate = selectCommand.CreateParameter();
            paramDate.ParameterName = "@CurrentDateTime";
            paramDate.DbType = DbType.Int64;
            paramDate.Value = _getTime.GetCurrentUtcDate().Ticks;
            selectCommand.Parameters.Add(paramDate);

            if (options.EnableRoute && routes != null && routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in routes)
                {
                    var param = selectCommand.CreateParameter();
                    param.ParameterName = "@Route" + routeCounter;
                    param.DbType = DbType.AnsiString;
                    param.Value = route;
                    selectCommand.Parameters.Add(param);
                    routeCounter++;
                }
            }
        }
    }
}
