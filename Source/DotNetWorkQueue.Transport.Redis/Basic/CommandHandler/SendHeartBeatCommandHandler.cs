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

using System;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long>,
        ICommandHandlerWithOutputAsync<SendHeartBeatCommand<string>, long>
    {
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IRedisConnection _connection;
        private readonly HeartBeatLua _heartBeatLua;

        /// <summary>Initializes a new instance of the <see cref="DeleteMessageCommandHandler"/> class.</summary>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="connection">Redis connection</param>
        /// <param name="heartBeatLua">Refreshes the claim only while it is still this caller's.</param>
        public SendHeartBeatCommandHandler(IUnixTimeFactory unixTimeFactory,
            IRedisConnection connection,
            HeartBeatLua heartBeatLua)
        {
            Guard.NotNull(unixTimeFactory);
            Guard.NotNull(connection);
            Guard.NotNull(heartBeatLua);

            _unixTimeFactory = unixTimeFactory;
            _heartBeatLua = heartBeatLua;
            _connection = connection;
        }

        /// <inheritdoc />
        public long Handle(SendHeartBeatCommand<string> command)
        {
            if (!CanBeat(command))
                return 0;

            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            //The script refreshes the score only while it is still the one this caller wrote, so a
            //message that has been reset and taken by another worker reports zero instead of having its
            //new owner's claim renewed. Asking whether the member merely exists cannot tell those apart
            //(GitHub #328).
            return _heartBeatLua.Execute(command.QueueId, date, Previous(command));
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(SendHeartBeatCommand<string> command)
        {
            if (!CanBeat(command))
                return 0;

            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            //see Handle: the refresh is conditional on the claim still being this caller's
            return await _heartBeatLua.ExecuteAsync(command.QueueId, date, Previous(command))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// The score this caller last wrote, as the working set stores it.
        /// </summary>
        /// <remarks>
        /// The command carries it as a DateTime because that is what the worker records; the working set
        /// keeps unix milliseconds, and the conversion is exact in both directions because the DateTime
        /// was built from those same milliseconds.
        /// </remarks>
        private static long? Previous(SendHeartBeatCommand<string> command) =>
            command.PreviousHeartBeat.HasValue
                ? (long)(command.PreviousHeartBeat.Value - UnixEpoch).TotalMilliseconds
                : (long?)null;

        //the same epoch BaseUnixTime converts against, so a value that came from the working set
        //converts back to exactly the score it was written as
        private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

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
