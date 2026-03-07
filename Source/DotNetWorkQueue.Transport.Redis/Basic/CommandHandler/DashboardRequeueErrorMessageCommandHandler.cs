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
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    internal class DashboardRequeueErrorMessageCommandHandler : ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>
    {
        private readonly DashboardRequeueErrorMessageLua _requeueLua;
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;
        private readonly IInternalSerializer _serializer;

        public DashboardRequeueErrorMessageCommandHandler(
            DashboardRequeueErrorMessageLua requeueLua,
            IRedisConnection connection,
            RedisNames redisNames,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => requeueLua, requeueLua);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => serializer, serializer);

            _requeueLua = requeueLua;
            _connection = connection;
            _redisNames = redisNames;
            _serializer = serializer;
        }

        public long Handle(DashboardRequeueErrorMessageCommand command)
        {
            var result = _requeueLua.Execute(command.MessageId);
            if (result == 1)
            {
                // Clear error tracking from the metadata so the message gets a fresh retry count
                var db = _connection.Connection.GetDatabase();
                var metaBytes = (byte[])db.HashGet(_redisNames.MetaData, command.MessageId);
                if (metaBytes != null && metaBytes.Length > 0)
                {
                    var meta = _serializer.ConvertBytesTo<RedisMetaData>(metaBytes);
                    if (meta?.ErrorTracking?.Errors?.Count > 0)
                    {
                        meta.ErrorTracking.Errors.Clear();
                        db.HashSet(_redisNames.MetaData, command.MessageId, _serializer.ConvertToBytes(meta));
                    }
                }
            }
            return result;
        }
    }
}
