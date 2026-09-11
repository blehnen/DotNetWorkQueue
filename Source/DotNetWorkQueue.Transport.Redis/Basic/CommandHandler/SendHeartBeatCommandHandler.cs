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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long>,
        ICommandHandlerWithOutputAsync<SendHeartBeatCommand<string>, long>
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
            Guard.NotNull(unixTimeFactory);
            Guard.NotNull(connection);
            Guard.NotNull(redisNames);

            _unixTimeFactory = unixTimeFactory;
            _connection = connection;
            _redisNames = redisNames;
        }

        /// <inheritdoc />
        public long Handle(SendHeartBeatCommand<string> command)
        {
            if (!CanBeat(command))
                return 0;

            var db = GetDb();
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            //SortedSetUpdate says whether the member was there to update. SortedSetAdd cannot: with
            //When.Exists it reports whether a member was *added*, which is never - so the result was
            //discarded and a message the monitor had already reclaimed still reported a fresh beat.
            if (!db.SortedSetUpdate(_redisNames.Working, command.QueueId, date, SortedSetWhen.Exists))
                return 0;

            return date;
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(SendHeartBeatCommand<string> command)
        {
            if (!CanBeat(command))
                return 0;

            var db = GetDb();
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            //see Handle: the update has to report whether the message was still in the working set
            if (!await db.SortedSetUpdateAsync(_redisNames.Working, command.QueueId, date, SortedSetWhen.Exists)
                    .ConfigureAwait(false))
                return 0;

            return date;
        }

        /// <summary>
        /// The database to beat against. Virtual so a test can supply one - <see cref="IRedisConnection"/>
        /// hands back a concrete multiplexer, which is the same reason WriteMessageHistoryHandler has this.
        /// </summary>
        protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();

        /// <summary>
        /// A disposed connection or a message with no id has nothing to beat for.
        /// </summary>
        private bool CanBeat(SendHeartBeatCommand<string> command)
        {
            return !_connection.IsDisposed && !string.IsNullOrWhiteSpace(command.QueueId);
        }
    }
}
