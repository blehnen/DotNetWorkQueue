// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class MoveDelayedRecordsCommandHandler : ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long>
    {
        private readonly MoveDelayedToPendingLua _moveDelayedToPendingLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedRecordsCommandHandler" /> class.
        /// </summary>
        /// <param name="moveDelayedToPendingLua">The move delayed to pending lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        public MoveDelayedRecordsCommandHandler(
            MoveDelayedToPendingLua moveDelayedToPendingLua,
            IUnixTimeFactory unixTimeFactory,
            RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => moveDelayedToPendingLua, moveDelayedToPendingLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);

            _moveDelayedToPendingLua = moveDelayedToPendingLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
        }

        /// <inheritdoc />
        public long Handle(MoveDelayedRecordsCommand command)
        {
            return command.Token.IsCancellationRequested
                ? 0
                : _moveDelayedToPendingLua.Execute(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds(),
                    _options.MoveDelayedMessagesBatchLimit);
        }
    }
}
