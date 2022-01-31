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

using System.Diagnostics;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;


namespace DotNetWorkQueue.Transport.Redis.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class RollbackMessageCommandHandlerDecorator : ICommandHandler<RollbackMessageCommand<string>>
    {
        private readonly ICommandHandler<RollbackMessageCommand<string>> _handler;
        private readonly ActivitySource _tracer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public RollbackMessageCommandHandlerDecorator(ICommandHandler<RollbackMessageCommand<string>> handler, ActivitySource tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public void Handle(RollbackMessageCommand<string> command)
        {
            //lets add a bit more information to the active span if possible
            if (Activity.Current != null)
            {
                if (command.IncreaseQueueDelay.HasValue)
                    Activity.Current.SetTag("MessageDelay",
                        command.IncreaseQueueDelay.Value.ToString());
            }
            _handler.Handle(command);
        }
    }
}
