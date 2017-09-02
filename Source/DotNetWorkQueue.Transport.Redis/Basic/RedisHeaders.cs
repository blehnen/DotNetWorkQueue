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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Defines our custom headers for internal operations
    /// </summary>
    internal class RedisHeaders
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHeaders" /> class.
        /// </summary>
        /// <param name="messageContextDataFactory">The message context data factory.</param>
        /// <param name="headers">The headers.</param>
        public RedisHeaders(IMessageContextDataFactory messageContextDataFactory,
            IHeaders headers)
        {
            Guard.NotNull(() => messageContextDataFactory, messageContextDataFactory);
            Guard.NotNull(() => headers, headers);
            Headers = headers;
            IncreaseQueueDelay = messageContextDataFactory.Create("IncreaseQueueDelay", new RedisQueueDelay(TimeSpan.Zero));
            CorrelationId = messageContextDataFactory.Create<RedisQueueCorrelationIdSerialized>("CorrelationId", null);
        }
        /// <summary>
        /// Gets the standard headers
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IHeaders Headers { get; }
        /// <summary>
        /// Gets the increase queue delay.
        /// </summary>
        /// <value>
        /// The increase queue delay.
        /// </value>
        /// <remarks>How much a record should be delayed when a rollback occurs</remarks>
        public IMessageContextData<RedisQueueDelay> IncreaseQueueDelay
        {
            get; 
        }
        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public IMessageContextData<RedisQueueCorrelationIdSerialized> CorrelationId { get;  } 
    }
}
