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
using System.Linq;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    internal class DashboardDeleteAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>
    {
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        public DashboardDeleteAllErrorMessagesCommandHandler(
            IRedisConnection connection,
            RedisNames redisNames)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _connection = connection;
            _redisNames = redisNames;
        }

        public long Handle(DashboardDeleteAllErrorMessagesCommand command)
        {
            var db = _connection.Connection.GetDatabase();

            // Get all error IDs
            var errorIds = db.ListRange(_redisNames.Error, 0, -1).Select(x => x.ToString()).ToList();
            if (errorIds.Count == 0) return 0;

            // Delete in batches to avoid blocking
            foreach (var id in errorIds)
            {
                db.HashDelete(_redisNames.MetaData, id);
                db.HashDelete(_redisNames.Values, id);
                db.HashDelete(_redisNames.Headers, id);
                db.HashDelete(_redisNames.Status, id);
                db.SortedSetRemove(_redisNames.ErrorTime, id);
            }

            // Clear the error list
            db.KeyDelete(_redisNames.Error);

            return errorIds.Count;
        }
    }
}
