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
using System.Threading.Tasks;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Sends a message to an RPC queue
    /// </summary>
    /// <typeparam name="TSendMessage">The type of the sent message.</typeparam>
    public interface IMessageProcessingRpcSend<in TSendMessage>
        where TSendMessage : class
    {
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        ISentMessage Handle(TSendMessage message, TimeSpan timeOut);

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        Task<ISentMessage> HandleAsync(TSendMessage message, TimeSpan timeOut);

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        ISentMessage Handle(TSendMessage message, IAdditionalMessageData data, TimeSpan timeOut);

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns></returns>
        Task<ISentMessage> HandleAsync(TSendMessage message, IAdditionalMessageData data, TimeSpan timeOut);
    }
}
