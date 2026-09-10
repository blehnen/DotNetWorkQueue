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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;

namespace DotNetWorkQueue.Transport.LiteDb.Trace.Decorator
{
    /// <summary>
    /// The asynchronous twin of <see cref="RollbackMessageCommandHandlerDecorator"/>.
    /// </summary>
    /// <remarks>
    /// A separate class rather than one type implementing both handler interfaces: a decorator whose
    /// constructor takes both services is decorated by itself on each of them, which SimpleInjector
    /// rejects as a cyclic graph. The tags themselves come from the synchronous decorator so there is
    /// still only one copy of them.
    /// </remarks>
    public class RollbackMessageCommandHandlerDecoratorAsync : ICommandHandlerAsync<RollbackMessageCommand<int>>
    {
        private readonly ICommandHandlerAsync<RollbackMessageCommand<int>> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandlerDecoratorAsync"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public RollbackMessageCommandHandlerDecoratorAsync(ICommandHandlerAsync<RollbackMessageCommand<int>> handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task HandleAsync(RollbackMessageCommand<int> command)
        {
            RollbackMessageCommandHandlerDecorator.AddTags(command);
            await _handler.HandleAsync(command).ConfigureAwait(false);
        }
    }
}
