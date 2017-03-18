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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Clears expired messages from the transport
    /// </summary>
    internal class ClearExpiredMessagesCommandHandler : ICommandHandlerWithOutput<ClearExpiredMessagesCommand, long>
    {
        private readonly ClearExpiredMessagesLua _clearExpiredMessagesLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearExpiredMessagesCommandHandler" /> class.
        /// </summary>
        /// <param name="clearExpiredMessagesLua">The clear expired messages.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        public ClearExpiredMessagesCommandHandler(ClearExpiredMessagesLua clearExpiredMessagesLua, 
            IUnixTimeFactory unixTimeFactory, 
            RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => clearExpiredMessagesLua, clearExpiredMessagesLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);

            _clearExpiredMessagesLua = clearExpiredMessagesLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(ClearExpiredMessagesCommand command)
        {
            return _clearExpiredMessagesLua.Execute(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds(), _options.ClearExpiredMessagesBatchLimit);
        }
    }
}
