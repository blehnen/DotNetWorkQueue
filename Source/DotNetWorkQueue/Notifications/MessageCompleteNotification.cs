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
    /// A message has completed processing 
    /// </summary>
    public class MessageCompleteNotification : ABaseNotification
    {
        /// <summary>
        /// A message has completed processing.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The message body.</param>
        public MessageCompleteNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, dynamic body) : base(id, correlationId, headers)
        {
            Body = body;
        }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <remarks>Only the message processor knows the type; If you need to access this data, you will need to cast it to your object type</remarks>
        public dynamic Body { get; }
    }
}
