// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.Shared.Basic.Query
{
    /// <summary>
    /// Represents a message that should be reset.
    /// </summary>
    public class MessageToReset<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageToReset{T}"/> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="heartBeat">The heart beat.</param>
        /// <param name="headers">The headers.</param>
        public MessageToReset(T queueId, DateTime heartBeat, IReadOnlyDictionary<string, object> headers)
        {
            QueueId = queueId;
            HeartBeat = heartBeat;
            Headers = headers;
        }
        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public T QueueId { get; }
        /// <summary>
        /// Gets the heart beat.
        /// </summary>
        /// <value>
        /// The heart beat.
        /// </value>
        /// <remarks>
        /// The queue record must have this value for the heartbeat, or it won't be updated.
        /// If the value has changed in the DB, this means that other process has reset the status, and a worker has started processing the record
        /// </remarks>
        public DateTime HeartBeat { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers { get; }
    }
}
