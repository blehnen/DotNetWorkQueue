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

using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    internal class BuildDequeueCommand
    {
        private readonly IGetTime _getTime;
        public BuildDequeueCommand(IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _getTime = getTimeFactory.Create();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal void BuildCommand(SQLiteCommand selectCommand, IMessageId messageId, CommandString commandString,
            SqLiteMessageQueueTransportOptions options, List<string> routes )
        {
            if (messageId != null && messageId.HasValue)
            {
                selectCommand.CommandText = commandString.CommandText;
                selectCommand.Parameters.Add("@QueueID", DbType.Int64);
                selectCommand.Parameters.Add("@CurrentDateTime", DbType.Int64);
                selectCommand.Parameters["@QueueID"].Value = messageId.Id.Value;
                selectCommand.Parameters["@CurrentDateTime"].Value =
                    _getTime.GetCurrentUtcDate().Ticks;
            }
            else
            {
                selectCommand.CommandText = commandString.CommandText;
                selectCommand.Parameters.Add("@CurrentDateTime", DbType.Int64);
                selectCommand.Parameters["@CurrentDateTime"].Value =
                    _getTime.GetCurrentUtcDate().Ticks;
            }

            if (options.EnableRoute && routes != null && routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in routes)
                {
                    selectCommand.Parameters.Add("@Route" + routeCounter.ToString(), DbType.AnsiString);
                    selectCommand.Parameters["@Route" + routeCounter.ToString()].Value = route;
                    routeCounter++;
                }
            }
        }
    }
}
