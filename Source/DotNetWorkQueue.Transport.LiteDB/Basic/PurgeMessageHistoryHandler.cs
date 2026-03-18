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
using DotNetWorkQueue.Transport.LiteDb.Schema;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Purges old message history records for LiteDB transport.
    /// </summary>
    public class PurgeMessageHistoryHandler : IPurgeMessageHistory
    {
        private readonly LiteDbConnectionManager _connectionManager;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PurgeMessageHistoryHandler"/> class.
        /// </summary>
        public PurgeMessageHistoryHandler(LiteDbConnectionManager connectionManager,
            TableNameHelper tableNameHelper)
        {
            _connectionManager = connectionManager;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public long Purge(DateTime olderThan)
        {
            var cutoffTicks = olderThan.Ticks;
            using (var db = _connectionManager.GetDatabase())
            {
                var col = db.Database.GetCollection<HistoryTable>(_tableNameHelper.HistoryName);
                return col.DeleteMany(x =>
                    (x.CompletedUtc > 0 && x.CompletedUtc < cutoffTicks) ||
                    (x.CompletedUtc == 0 && x.EnqueuedUtc < cutoffTicks));
            }
        }
    }
}
