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

namespace DotNetWorkQueue
{
    /// <summary>
    /// An internal message for a queue
    /// </summary>
    /// <remarks>This interface exists so that the internal queue classes do not need to care about the message type <see cref="Body"/>. This allows
    /// us to pass the message body around with caring about it's type.  This requires reflection to translate the message to <see cref="IReceivedMessage{T}"/>
    /// See <see cref="IGenerateReceivedMessage"/> for how the message is switched to the external type. </remarks>
    public interface IReceivedMessageInternal
    {
        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        dynamic Body { get; }
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        IMessageId MessageId { get; }
        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        ICorrelationId CorrelationId { get; }
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        IReadOnlyDictionary<string, object> Headers { get; }
    }
}
