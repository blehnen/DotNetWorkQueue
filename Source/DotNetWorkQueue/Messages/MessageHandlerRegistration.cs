// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Registers the type of message that will be returned from a consuming queue.
    /// </summary>
    public class MessageHandlerRegistration: IMessageHandlerRegistration
    {
        private object _handler;
        private Type _messageType;
        private readonly IGenerateReceivedMessage _generateMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerRegistration"/> class.
        /// </summary>
        /// <param name="generateMessage">The generate message.</param>
        public MessageHandlerRegistration(IGenerateReceivedMessage generateMessage)
        {
            Guard.NotNull(() => generateMessage, generateMessage);
            _generateMessage = generateMessage;
        }

        /// <summary>
        /// Sets the specified message action.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageAction">The message action.</param>
        public void Set<T>(Action<IReceivedMessage<T>, IWorkerNotification> messageAction) where T : class
        {
            Guard.NotNull(() => messageAction, messageAction);
            _handler = messageAction;
            _messageType = typeof (T);
        }

        /// <summary>
        /// Retrieves the handler added via <see cref="Set{T}" />
        /// </summary>
        /// <returns>
        /// The handler
        /// </returns>
        public dynamic GetHandler()
        {
            return _handler;
        }

        /// <summary>
        /// Translates the message into a format that the user can process
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public dynamic GenerateMessage(IReceivedMessageInternal message)
        {
            return _generateMessage.GenerateMessage(_messageType, message);
        }
    }
}
