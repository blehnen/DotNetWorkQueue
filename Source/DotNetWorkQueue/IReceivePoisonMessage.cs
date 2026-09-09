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
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Handles poison messages.
    /// </summary>
    /// <remarks>A poison message is a message that can be de-queued, but can't be re-assembled.</remarks>
    public interface IReceivePoisonMessage
    {
        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occurred during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        void Handle(IMessageContext context, PoisonMessageException exception);

        /// <summary>
        /// Moves a message that cannot be processed to the error queue, without blocking a thread.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="exception">The exception that made the message poison.</param>
        /// <remarks>
        /// Called from the asynchronous consumer, where the synchronous twin would block a thread-pool
        /// thread: after the receive became awaitable this runs in a continuation rather than on the
        /// worker's own dedicated thread.
        /// </remarks>
        Task HandleAsync(IMessageContext context, PoisonMessageException exception);
    }
}
