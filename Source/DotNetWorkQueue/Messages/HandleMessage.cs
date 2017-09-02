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

using System.Threading.Tasks;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Handles running invoking the user provided delegate
    /// </summary>
    internal class HandleMessage : IHandleMessage
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IMessageHandlerAsync _messageHandlerAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleMessage" /> class.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="messageHandlerAsync">The message handler asynchronous.</param>
        public HandleMessage(IMessageHandler messageHandler,
            IMessageHandlerAsync messageHandlerAsync)
        {
            Guard.NotNull(() => messageHandler, messageHandler);
            Guard.NotNull(() => messageHandlerAsync, messageHandlerAsync);

            _messageHandler = messageHandler;
            _messageHandlerAsync = messageHandlerAsync;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        /// <returns>
        /// A task if async mode; null if sync mode
        /// </returns>
        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            _messageHandler.Handle(message, workerNotification);
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        /// <returns>
        /// A task
        /// </returns>
        public async Task HandleAsync(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
             await _messageHandlerAsync.HandleAsync(message, workerNotification).ConfigureAwait(false);
        }
    }
}
