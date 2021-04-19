// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendHeartBeatCommandHandler: ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long>
    {
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IRedisConnection _connection;
        private readonly RedisNames _redisNames;

        /// <summary>Initializes a new instance of the <see cref="DeleteMessageCommandHandler"/> class.</summary>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="connection">Redis connection</param>
        /// <param name="redisNames">Redis key names</param>
        public SendHeartBeatCommandHandler(IUnixTimeFactory unixTimeFactory,
            IRedisConnection connection, 
            RedisNames redisNames)
        {
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);

            _unixTimeFactory = unixTimeFactory;
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Handle(SendHeartBeatCommand<string> command)
        {
            if (_connection.IsDisposed)
                return 0;

            if (string.IsNullOrWhiteSpace(command.QueueId))
                return 0;

            var db = _connection.Connection.GetDatabase();
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            db.SortedSetAdd(_redisNames.Working, command.QueueId, date, When.Exists);

            return date;
        }
    }
}
