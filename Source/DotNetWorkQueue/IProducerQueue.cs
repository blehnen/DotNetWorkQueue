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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a queue that can send messages
    /// </summary>
    /// <typeparam name="TMessage">The message Type</typeparam>
    public interface IProducerQueue<TMessage> : IProducerBaseQueue
        where TMessage: class
    {
        /// <summary>
        /// Sends the specified message. Additional message meta data is optional.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        IQueueOutputMessage Send(TMessage message, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<TMessage> messages);

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<QueueMessage<TMessage, IAdditionalMessageData>> messages);

        /// <summary>
        /// Sends the specified message. Additional message meta data is optional.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        Task<IQueueOutputMessage> SendAsync(TMessage message, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<TMessage> messages);

        /// <summary>
        /// Sends the specified messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<QueueMessage<TMessage, IAdditionalMessageData>> messages);
    }
}
