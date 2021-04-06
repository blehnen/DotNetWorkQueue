// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <inheritdoc />
    public class SendHeartBeat<T> : ISendHeartBeat
        where T : struct, IComparable<T>
    {
        #region Member Level Variables
        private readonly ICommandHandlerWithOutput<SendHeartBeatCommand<T>, DateTime?> _commandHandler;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeat{T}" /> class.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        public SendHeartBeat(ICommandHandlerWithOutput<SendHeartBeatCommand<T>, DateTime?> commandHandler)
        {
            Guard.NotNull(() => commandHandler, commandHandler);
            _commandHandler = commandHandler;
        }
        #endregion

        #region ISendHeartBeat
        /// <inheritdoc />
        public IHeartBeatStatus Send(IMessageContext context)
        {
            var command = new SendHeartBeatCommand<T>((T)context.MessageId.Id.Value);
            var oDate = _commandHandler.Handle(command);
            return new HeartBeatStatus(new MessageQueueId<T>(command.QueueId), oDate);
        }
        #endregion
    }
}
