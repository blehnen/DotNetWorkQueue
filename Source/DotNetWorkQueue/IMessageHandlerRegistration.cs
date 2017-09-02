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

using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Registers the type of message that will be returned from a consuming queue.
    /// </summary>
    public interface IMessageHandlerRegistration
    {
        /// <summary>
        /// Sets the specified message action.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageAction">The message action.</param>
        void Set<T>(Action<IReceivedMessage<T>, IWorkerNotification> messageAction)
             where T : class;

        /// <summary>
        /// Retrieves the handler added via <see cref="Set{T}" />
        /// </summary>
        /// <returns>
        /// The handler as "Action{IReceivedMessage{T}, IWorkerNotification}" />
        /// </returns>
        dynamic GetHandler();

        /// <summary>
        /// Translates the message into a format that the user can process
        /// </summary>
        /// <param name="message">The message to convert.</param>
        /// <returns>A <see cref="IReceivedMessage{T}"/></returns>
        dynamic GenerateMessage(IReceivedMessageInternal message);
    }
}
