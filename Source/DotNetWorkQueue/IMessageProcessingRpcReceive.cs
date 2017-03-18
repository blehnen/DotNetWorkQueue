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
    /// Receives a response from an RPC request.
    /// </summary>
    /// <typeparam name="TReceivedMessage">The type of the received message.</typeparam>
    public interface IMessageProcessingRpcReceive<TReceivedMessage>
         where TReceivedMessage : class
    {
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="queueWait">The queue wait.</param>
        /// <returns></returns>
        /// <exception cref="System.TimeoutException"></exception>
        IReceivedMessage<TReceivedMessage> Handle(IMessageId messageId, TimeSpan timeOut, IQueueWait queueWait);

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="queueWait">The queue wait.</param>
        /// <returns></returns>
        /// <exception cref="System.TimeoutException"></exception>
        Task<IReceivedMessage<TReceivedMessage>> HandleAsync(IMessageId messageId, TimeSpan timeOut,
            IQueueWait queueWait);
    }
}
