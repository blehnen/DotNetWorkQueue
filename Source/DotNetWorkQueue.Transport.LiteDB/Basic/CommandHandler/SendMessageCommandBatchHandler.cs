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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Sends a batch of messages in a single transaction.
    /// </summary>
    /// <remarks>The work is in <see cref="SendMessageCommandBatchShared"/>.</remarks>
    internal class SendMessageCommandBatchHandler : ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly SendMessageCommandBatchShared _shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandler"/> class.
        /// </summary>
        /// <param name="shared">The batch implementation.</param>
        public SendMessageCommandBatchHandler(SendMessageCommandBatchShared shared)
        {
            Guard.NotNull(shared);
            _shared = shared;
        }

        /// <inheritdoc />
        public QueueOutputMessages Handle(SendMessageCommandBatch command) => _shared.Handle(command);
    }

    /// <summary>
    /// Asynchronous counterpart to <see cref="SendMessageCommandBatchHandler"/>.
    /// </summary>
    /// <remarks>
    /// LiteDB has no asynchronous API, so this runs the same work on a task — the same approach
    /// <c>SendMessageCommandHandlerAsync</c> takes for a single message.
    /// </remarks>
    internal class SendMessageCommandBatchHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly SendMessageCommandBatchShared _shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandlerAsync"/> class.
        /// </summary>
        /// <param name="shared">The batch implementation.</param>
        public SendMessageCommandBatchHandlerAsync(SendMessageCommandBatchShared shared)
        {
            Guard.NotNull(shared);
            _shared = shared;
        }

        /// <inheritdoc />
        public async Task<QueueOutputMessages> HandleAsync(SendMessageCommandBatch command) =>
            await Task.Run(() => _shared.Handle(command)).ConfigureAwait(false);
    }
}
