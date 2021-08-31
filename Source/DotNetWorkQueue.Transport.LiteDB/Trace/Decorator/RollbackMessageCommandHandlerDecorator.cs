﻿// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Transport.LiteDb.Trace.Decorator
{
    /// <summary>
    /// Tracing for rolling back a message
    /// </summary>
    public class RollbackMessageCommandHandlerDecorator : ICommandHandler<RollbackMessageCommand<int>>
    {
        private readonly ICommandHandler<RollbackMessageCommand<int>> _handler;
        private readonly Tracer _tracer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public RollbackMessageCommandHandlerDecorator(ICommandHandler<RollbackMessageCommand<int>> handler, Tracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public void Handle(RollbackMessageCommand<int> command)
        {
            //lets add a bit more information to the active span if possible
            if (OpenTelemetry.Trace.Tracer.CurrentSpan != null)
            {
                if (command.IncreaseQueueDelay.HasValue)
                    OpenTelemetry.Trace.Tracer.CurrentSpan.SetAttribute("MessageDelay",
                        command.IncreaseQueueDelay.Value.ToString());

                if (command.LastHeartBeat.HasValue)
                    OpenTelemetry.Trace.Tracer.CurrentSpan.SetAttribute("LastHeartBeatValue",
                        command.LastHeartBeat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            _handler.Handle(command);
        }
    }
}
