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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using OpenTracing;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class RollbackMessageCommandHandlerDecorator : ICommandHandler<RollbackMessageCommand>
    {
        private readonly ICommandHandler<RollbackMessageCommand> _handler;
        private readonly ITracer _tracer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public RollbackMessageCommandHandlerDecorator(ICommandHandler<RollbackMessageCommand> handler, ITracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public void Handle(RollbackMessageCommand command)
        {
            //lets add a bit more information to the active span if possible
            if (_tracer.ActiveSpan != null)
            {
                if (command.IncreaseQueueDelay.HasValue)
                    _tracer.ActiveSpan.SetTag("MessageDelay",
                        command.IncreaseQueueDelay.Value.ToString());

                if (command.LastHeartBeat.HasValue)
                    _tracer.ActiveSpan.SetTag("LastHeartBeatValue",
                        command.LastHeartBeat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            _handler.Handle(command);
        }
    }
}
