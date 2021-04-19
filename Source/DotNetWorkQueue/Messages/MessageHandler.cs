// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Wraps the action for handling a message; this is provided by the caller of the library.
    /// </summary>
    internal class MessageHandler: IMessageHandler
    {
        private readonly IMessageHandlerRegistration _messageHandlerRegistration;
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandler"/> class.
        /// </summary>
        /// <param name="messageHandlerRegistration">The message handler registration.</param>
        public MessageHandler(IMessageHandlerRegistration messageHandlerRegistration)
        {
            Guard.NotNull(() => messageHandlerRegistration, messageHandlerRegistration);
            _messageHandlerRegistration = messageHandlerRegistration;
        }
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => workerNotification, workerNotification);
            _messageHandlerRegistration.GetHandler().Invoke(_messageHandlerRegistration.GenerateMessage(message), workerNotification);
        }
    }
}
