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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// A received message
    /// </summary>
    /// <typeparam name="T">The type of the message</typeparam>
    public class ReceivedMessage<T> : IReceivedMessage<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IReceivedMessage{T}" /> class.
        /// </summary>
        /// <param name="message">The internal message.</param>
        public ReceivedMessage(IReceivedMessageInternal message)
        {
            Guard.NotNull(() => message, message);

            Body = (T)message.Body;
            Headers = new ReadOnlyDictionary<string, object>(message.Headers.ToDictionary(entry => entry.Key,
                                               entry => entry.Value));
            MessageId = message.MessageId;
            CorrelationId = message.CorrelationId;
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public IMessageId MessageId { get; }
        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public ICorrelationId CorrelationId { get; }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public T Body { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Returns typed data from the headers collection
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class
        {
            if (Headers.ContainsKey(property.Name))
            {
                return (THeader) Headers[property.Name];
            }
            return property.Default;
        }
    }
}
