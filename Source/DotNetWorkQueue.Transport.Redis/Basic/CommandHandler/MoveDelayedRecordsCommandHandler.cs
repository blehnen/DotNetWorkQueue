// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Moves delayed messages into the pending queue
    /// </summary>
    internal class MoveDelayedRecordsCommandHandler : ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long>
    {
        private readonly MoveDelayedToPendingLua _moveDelayedToPendingLua;
        private readonly bool _rpcQueue;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveDelayedRecordsCommandHandler" /> class.
        /// </summary>
        /// <param name="moveDelayedToPendingLua">The move delayed to pending lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        /// <param name="queueContext">The queue context.</param>
        public MoveDelayedRecordsCommandHandler( 
            MoveDelayedToPendingLua moveDelayedToPendingLua, 
            IUnixTimeFactory unixTimeFactory, 
            RedisQueueTransportOptions options,
            QueueContext queueContext)
        {
            Guard.NotNull(() => moveDelayedToPendingLua, moveDelayedToPendingLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => queueContext, queueContext);

            _moveDelayedToPendingLua = moveDelayedToPendingLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
            _rpcQueue = queueContext.Context == QueueContexts.RpcQueue;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(MoveDelayedRecordsCommand command)
        {
            return command.Token.IsCancellationRequested 
                ? 
                    0 
                :
                    _moveDelayedToPendingLua.Execute(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds(), _options.MoveDelayedMessagesBatchLimit, _rpcQueue);
        }
    }
}
