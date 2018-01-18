// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
    /// A queue that can be used to send a message and receive a response
    /// </summary>
    /// <typeparam name="TReceivedMessage">The message type of the response</typeparam>
    /// <typeparam name="TSendMessage">The type of the message to send</typeparam>
    public interface IRpcQueue<TReceivedMessage, in TSendMessage> : IRpcBaseQueue
        where TReceivedMessage : class
        where TSendMessage : class
    {
        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        IReceivedMessage<TReceivedMessage> Send(TSendMessage message, TimeSpan timeOut, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        Task<IReceivedMessage<TReceivedMessage>> SendAsync(TSendMessage message, TimeSpan timeOut, IAdditionalMessageData data = null);
    }
}
