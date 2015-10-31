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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a queue that sends a message to an RPC queue.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    public interface IProducerQueueRpc<in TMessage> : IDisposable, IIsDisposed
        where TMessage: class
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        QueueProducerConfiguration Configuration { get; }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        IQueueOutputMessage Send(TMessage message, IResponseId responseId, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        Task<IQueueOutputMessage> SendAsync(TMessage message, IResponseId responseId, IAdditionalMessageData data = null);

        /// <summary>
        /// Creates a response instance for sending a message reply
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="timeOut">The message time out.</param>
        /// <returns></returns>
        IResponseId CreateResponse(IMessageId messageId, TimeSpan timeOut);
    }
}
