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

using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Updates the heart beat for a work in progress item
    /// </summary>
    internal class SendHeartBeatCommandHandler: ICommandHandlerWithOutput<SendHeartBeatCommand, long>
    {
        private readonly SendHeartbeatLua _sendHeartbeatLua;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="sendHeartbeatLua">The sendheartbeat.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public SendHeartBeatCommandHandler(SendHeartbeatLua sendHeartbeatLua, 
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => sendHeartbeatLua, sendHeartbeatLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);

            _sendHeartbeatLua = sendHeartbeatLua;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(SendHeartBeatCommand command)
        {
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            _sendHeartbeatLua.Execute(command.QueueId.Id.Value.ToString(), date);
            return date;
        }
    }
}
