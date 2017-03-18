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
using System;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    internal class SqLiteMessageQueueSendHeartBeat : ISendHeartBeat 
    {
        #region Member Level Variables
        private readonly ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?> _commandHandler;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueSendHeartBeat" /> class.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        public SqLiteMessageQueueSendHeartBeat(ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?> commandHandler)
        {
            Guard.NotNull(() => commandHandler, commandHandler);
            _commandHandler = commandHandler;
        }
        #endregion

        #region ISendHeartBeat
        /// <summary>
        /// Updates the heart beat for a record context
        /// </summary>
        /// <param name="context">The context.</param>
        public IHeartBeatStatus Send(IMessageContext context)
        {
            var command = new SendHeartBeatCommand((long)context.MessageId.Id.Value);
            var oDate = _commandHandler.Handle(command);
            return new HeartBeatStatus(new SqLiteMessageQueueId(command.QueueId), oDate);
        }
        #endregion
    }
}
