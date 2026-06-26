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
using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Shared.Basic.Command;

namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <summary>
    /// Placeholder batch-send handler registered for transports that do not implement a true
    /// bulk insert, purely to satisfy the <c>SendMessages&lt;T&gt;</c> constructor dependency.
    /// </summary>
    /// <remarks>
    /// Its <see cref="Handle"/> / <see cref="HandleAsync"/> methods are never invoked in normal
    /// operation: such transports report <see cref="ISendMessageBatchSupport.IsSupported"/> =
    /// <c>false</c>, so <c>SendMessages&lt;T&gt;</c> uses the per-message loop and never dispatches
    /// here. They throw rather than return an empty result so that an accidental wiring change
    /// fails loudly instead of silently dropping messages.
    /// </remarks>
    public class NoOpSendMessageCommandBatchHandler :
        ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>,
        ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>
    {
        /// <inheritdoc />
        public QueueOutputMessages Handle(SendMessageCommandBatch command)
        {
            throw new NotSupportedException(
                "This transport does not provide a batch send handler; callers must use the " +
                "per-message send loop. Dispatching to the no-op batch handler indicates a " +
                "registration error.");
        }

        /// <inheritdoc />
        public Task<QueueOutputMessages> HandleAsync(SendMessageCommandBatch command)
        {
            return Task.FromException<QueueOutputMessages>(new NotSupportedException(
                "This transport does not provide a batch send handler; callers must use the " +
                "per-message send loop. Dispatching to the no-op batch handler indicates a " +
                "registration error."));
        }
    }
}
