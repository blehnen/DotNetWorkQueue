// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// Base message notification
    /// </summary>
    public abstract class ABaseNotification
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Message Id</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="headers">Message headers</param>
        protected ABaseNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers)
        {
            MessageId = id;
            CorrelationId = correlationId;
            Headers = headers;
        }

        /// <summary>
        /// Returns data from a header property
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class
        {
            if (!Headers.ContainsKey(property.Name))
            {
                return property.Default;
            }
            return (THeader)Headers[property.Name];
        }

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        /// <remarks>Can be null in some cases</remarks>
        public IMessageId MessageId { get; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        /// <remarks>Can be null in some cases</remarks>
        public ICorrelationId CorrelationId { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        /// <remarks>If possible use <seealso cref="GetHeader{THeader}"/> to get data in a type safe manner</remarks>
        public IReadOnlyDictionary<string, object> Headers { get; }
    }
}
