﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
    /// Registers the callback and type of message for processing
    /// </summary>
    public class RegisterMessages: IRegisterMessages
    {
        private readonly IMessageHandlerRegistration _messageHandlerRegistration;
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterMessages"/> class.
        /// </summary>
        /// <param name="messageHandlerRegistration">The message handler registration.</param>
        public RegisterMessages(IMessageHandlerRegistration messageHandlerRegistration)
        {
            Guard.NotNull(() => messageHandlerRegistration, messageHandlerRegistration);

            _messageHandlerRegistration = messageHandlerRegistration;
        }
        /// <summary>
        /// Registers the specified message action.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="messageAction">The message action.</param>
        public void Register<TMessage>(Action<IReceivedMessage<TMessage>, IWorkerNotification> messageAction)  
            where TMessage : class
        {
            Guard.NotNull(() => messageAction, messageAction);
            _messageHandlerRegistration.Set(messageAction);
        }
    }
}
