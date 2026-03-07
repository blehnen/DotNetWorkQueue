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
using System.Linq;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    internal class DashboardRequeueAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardRequeueAllErrorMessagesCommand, long>
    {
        private readonly DashboardRequeueAllErrorMessagesLua _requeueAllLua;
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _serializer;

        public DashboardRequeueAllErrorMessagesCommandHandler(
            DashboardRequeueAllErrorMessagesLua requeueAllLua,
            IRedisConnection connection,
            RedisNames redisNames,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => requeueAllLua, requeueAllLua);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => serializer, serializer);

            _requeueAllLua = requeueAllLua;
            _connection = connection;
            _redisNames = redisNames;
            _serializer = serializer;
        }

        public long Handle(DashboardRequeueAllErrorMessagesCommand command)
        {
            var db = _connection.Connection.GetDatabase();

            // Capture error IDs before the Lua script deletes the error list
            var errorIds = db.ListRange(_redisNames.Error, 0, -1).Select(x => x.ToString()).ToList();

            var count = _requeueAllLua.Execute();
            if (count > 0)
            {
                // Clear error tracking from metadata for all requeued messages
                foreach (var id in errorIds)
                {
                    var metaBytes = (byte[])db.HashGet(_redisNames.MetaData, id);
                    if (metaBytes != null && metaBytes.Length > 0)
                    {
                        var meta = _serializer.ConvertBytesTo<RedisMetaData>(metaBytes);
                        if (meta?.ErrorTracking?.Errors?.Count > 0)
                        {
                            meta.ErrorTracking.Errors.Clear();
                            db.HashSet(_redisNames.MetaData, id, _serializer.ConvertToBytes(meta));
                        }
                    }
                }
            }
            return count;
        }
    }
}
