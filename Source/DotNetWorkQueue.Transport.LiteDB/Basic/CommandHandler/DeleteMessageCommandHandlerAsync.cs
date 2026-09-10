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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <remarks>
    /// LiteDB has no asynchronous API, so this runs the synchronous handler on the thread pool - the
    /// same hack <see cref="SendMessageCommandHandlerAsync"/> uses. It keeps the caller's thread free,
    /// which is what the asynchronous consumer needs; it does not make the work asynchronous. Issue
    /// #283 tracks replacing this once LiteDB v6 ships real async methods.
    /// </remarks>
    internal class DeleteMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<DeleteMessageCommand<int>, long>
    {
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand<int>, long> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandlerAsync"/> class.
        /// </summary>
        /// <param name="handler">The synchronous handler that does the work.</param>
        public DeleteMessageCommandHandlerAsync(ICommandHandlerWithOutput<DeleteMessageCommand<int>, long> handler)
        {
            Guard.NotNull(handler);
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(DeleteMessageCommand<int> command)
        {
            return await Task.Run(() => _handler.Handle(command)).ConfigureAwait(false);
        }
    }
}
